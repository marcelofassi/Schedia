using Microsoft.EntityFrameworkCore;
using Schedia.Api.Contracts;
using Schedia.Api.Data;
using Schedia.Api.Data.Entities;
using Schedia.Api.Google;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SchediaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection("GoogleCalendar"));
builder.Services.AddSingleton<GoogleAuthHelper>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

app.UseHttpsRedirection();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
    .WithName("Healthcheck")
    .WithOpenApi();

app.MapGet("/api/meta", async (SchediaDbContext db, int hostId, string? lang) =>
{
    var langCode = string.IsNullOrWhiteSpace(lang) ? "es" : lang.Trim().ToLowerInvariant();

    var host = await db.Hosts
        .AsNoTracking()
        .FirstOrDefaultAsync(h => h.HostId == hostId && h.IsActive);

    if (host is null)
    {
        return Results.NotFound();
    }

    var legalText = await db.LegalTexts
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Lang == langCode && x.IsActive);

    if (legalText is null)
    {
        return Results.NotFound();
    }

    var response = new MetaResponse
    {
        HostId = host.HostId,
        HostName = host.DisplayName,
        HostRoleTitle = host.RoleTitle ?? string.Empty,
        Lang = langCode,
        LegalTextId = legalText.LegalTextId,
        LegalTextVersion = legalText.VersionLabel,
        LegalTextBody = legalText.Body
    };

    return Results.Ok(response);
})
.WithName("GetMeta")
.WithOpenApi();

app.MapPost("/api/availability", async (SchediaDbContext db, AvailabilityRequest request) =>
{
    if (request.RangeEnd <= request.RangeStart)
    {
        return Results.BadRequest();
    }

    if (request.DurationMinutes is not (30 or 45 or 60))
    {
        return Results.BadRequest();
    }

    var host = await db.Hosts
        .AsNoTracking()
        .FirstOrDefaultAsync(h => h.HostId == request.HostId && h.IsActive);

    if (host is null)
    {
        return Results.NotFound();
    }

    var nowUtc = DateTime.UtcNow;
    var minNoticeUtc = nowUtc.AddHours(4);
    var maxHorizonUtc = nowUtc.AddDays(30);

    var rangeStartUtc = request.RangeStart.UtcDateTime > minNoticeUtc
        ? request.RangeStart.UtcDateTime
        : minNoticeUtc;
    var rangeEndUtc = request.RangeEnd.UtcDateTime < maxHorizonUtc
        ? request.RangeEnd.UtcDateTime
        : maxHorizonUtc;

    if (rangeEndUtc <= rangeStartUtc)
    {
        return Results.Ok(new AvailabilityResponse { Slots = Array.Empty<AvailableSlot>() });
    }

    TimeZoneInfo hostTimeZone;
    try
    {
        hostTimeZone = TimeZoneInfo.FindSystemTimeZoneById(host.TimeZoneId);
    }
    catch
    {
        hostTimeZone = TimeZoneInfo.Utc;
    }

    var bookedRanges = await db.Bookings
        .AsNoTracking()
        .Where(b => b.HostId == request.HostId &&
                    b.Status == "booked" &&
                    b.StartUtc < rangeEndUtc &&
                    b.EndUtc > rangeStartUtc)
        .Select(b => new { b.StartUtc, b.EndUtc })
        .ToListAsync();

    var calendarBusyRanges = new List<(DateTime StartUtc, DateTime EndUtc)>();
    var hasGoogle = app.Services.GetRequiredService<GoogleAuthHelper>().IsConfigured();

    if (hasGoogle)
    {
        var googleAuth = app.Services.GetRequiredService<GoogleAuthHelper>();
        var googleOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<GoogleCalendarOptions>>().Value;
        var credential = googleAuth.CreateCredential(host.Email);
        var calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = googleOptions.ApplicationName
        });

        var freebusyRequest = new FreeBusyRequest
        {
            TimeMinDateTimeOffset = rangeStartUtc,
            TimeMaxDateTimeOffset = rangeEndUtc,
            TimeZone = "UTC",
            Items = new List<FreeBusyRequestItem>
            {
                new FreeBusyRequestItem { Id = host.CalendarId }
            }
        };

        var freebusyResponse = await calendarService.Freebusy.Query(freebusyRequest).ExecuteAsync();
        if (freebusyResponse?.Calendars != null &&
            freebusyResponse.Calendars.TryGetValue(host.CalendarId, out var calendar))
        {
            foreach (var busy in calendar.Busy)
            {
                if (busy.StartDateTimeOffset.HasValue && busy.EndDateTimeOffset.HasValue)
                {
                    calendarBusyRanges.Add((busy.StartDateTimeOffset.Value.UtcDateTime, busy.EndDateTimeOffset.Value.UtcDateTime));
                }
            }
        }

        var eventsRequest = calendarService.Events.List(host.CalendarId);
        eventsRequest.TimeMinDateTimeOffset = rangeStartUtc;
        eventsRequest.TimeMaxDateTimeOffset = rangeEndUtc;
        eventsRequest.SingleEvents = true;
        eventsRequest.ShowDeleted = false;
        eventsRequest.MaxResults = 250;

        var events = await eventsRequest.ExecuteAsync();
        if (events?.Items != null)
        {
            foreach (var calendarEvent in events.Items)
            {
                if (calendarEvent.EventType != "outOfOffice")
                {
                    continue;
                }

                var start = calendarEvent.Start?.DateTimeDateTimeOffset;
                var end = calendarEvent.End?.DateTimeDateTimeOffset;
                if (start.HasValue && end.HasValue)
                {
                    calendarBusyRanges.Add((start.Value.UtcDateTime, end.Value.UtcDateTime));
                }
            }
        }
    }

    var slots = new List<AvailableSlot>();
    var duration = TimeSpan.FromMinutes(request.DurationMinutes);
    var buffer = TimeSpan.FromMinutes(15);

    var rangeStartLocal = TimeZoneInfo.ConvertTimeFromUtc(rangeStartUtc, hostTimeZone);
    var rangeEndLocal = TimeZoneInfo.ConvertTimeFromUtc(rangeEndUtc, hostTimeZone);
    var currentDate = rangeStartLocal.Date;
    var endDate = rangeEndLocal.Date;

    while (currentDate <= endDate)
    {
        var dayOfWeek = currentDate.DayOfWeek;
        if (dayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday or DayOfWeek.Thursday or DayOfWeek.Friday)
        {
            var dayStartLocal = currentDate.AddHours(8);
            var dayEndLocal = currentDate.AddHours(18);

            var windowStart = dayStartLocal < rangeStartLocal ? rangeStartLocal : dayStartLocal;
            var windowEnd = dayEndLocal > rangeEndLocal ? rangeEndLocal : dayEndLocal;

            var slotStartLocal = windowStart;

            while (slotStartLocal + duration <= windowEnd)
            {
                var slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(slotStartLocal, hostTimeZone);
                var slotEndUtc = TimeZoneInfo.ConvertTimeToUtc(slotStartLocal + duration, hostTimeZone);
                var overlaps = bookedRanges.Any(b =>
                    slotStartUtc < b.EndUtc.Add(buffer) &&
                    slotEndUtc > b.StartUtc.Subtract(buffer));

                if (!overlaps && calendarBusyRanges.Count > 0)
                {
                    overlaps = calendarBusyRanges.Any(b =>
                        slotStartUtc < b.EndUtc.Add(buffer) &&
                        slotEndUtc > b.StartUtc.Subtract(buffer));
                }

                if (!overlaps)
                {
                    slots.Add(new AvailableSlot
                    {
                        Start = new DateTimeOffset(slotStartUtc, TimeSpan.Zero),
                        End = new DateTimeOffset(slotEndUtc, TimeSpan.Zero)
                    });
                }

                slotStartLocal = slotStartLocal.Add(duration);
            }
        }

        currentDate = currentDate.AddDays(1);
    }

    return Results.Ok(new AvailabilityResponse { Slots = slots });
})
.WithName("GetAvailability")
.WithOpenApi();

app.MapPost("/api/book", async (SchediaDbContext db, BookRequest request) =>
{
    if (request.DurationMinutes is not (30 or 45 or 60))
    {
        return Results.BadRequest();
    }

    var nowUtc = DateTime.UtcNow;

    var existingKey = await db.BookingIdempotency
        .AsNoTracking()
        .Include(x => x.Booking)
        .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey &&
                                  x.ExpiresAtUtc > nowUtc);

    if (existingKey?.Booking is not null)
    {
        return Results.Ok(new BookResponse
        {
            BookingId = existingKey.Booking.BookingId,
            GoogleEventId = existingKey.Booking.GoogleEventId ?? string.Empty,
            GoogleMeetLink = existingKey.Booking.GoogleMeetLink ?? string.Empty
        });
    }

    if (string.IsNullOrWhiteSpace(request.Client.Name) ||
        request.Client.Name.Length > 120 ||
        string.IsNullOrWhiteSpace(request.Client.Email) ||
        request.Client.Email.Length > 254 ||
        string.IsNullOrWhiteSpace(request.Client.Company) ||
        request.Client.Company.Length > 120)
    {
        return Results.BadRequest();
    }

    if (request.Client.Phone?.Length > 40 || request.Client.Reason?.Length > 400)
    {
        return Results.BadRequest();
    }

    try
    {
        var emailAddress = new System.Net.Mail.MailAddress(request.Client.Email);
        if (!string.Equals(emailAddress.Address, request.Client.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest();
        }
    }
    catch
    {
        return Results.BadRequest();
    }

    var host = await db.Hosts
        .AsNoTracking()
        .FirstOrDefaultAsync(h => h.HostId == request.HostId && h.IsActive);

    if (host is null)
    {
        return Results.NotFound();
    }

    var legalText = await db.LegalTexts
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.LegalTextId == request.LegalTextId && x.IsActive);

    if (legalText is null)
    {
        return Results.BadRequest();
    }

    var slotStartUtc = request.SlotStart.UtcDateTime;
    var slotEndUtc = slotStartUtc.AddMinutes(request.DurationMinutes);
    var minNoticeUtc = nowUtc.AddHours(4);
    var maxHorizonUtc = nowUtc.AddDays(30);

    if (slotStartUtc < minNoticeUtc || slotStartUtc > maxHorizonUtc)
    {
        return Results.BadRequest();
    }

    TimeZoneInfo hostTimeZone;
    try
    {
        hostTimeZone = TimeZoneInfo.FindSystemTimeZoneById(host.TimeZoneId);
    }
    catch
    {
        hostTimeZone = TimeZoneInfo.Utc;
    }

    var slotStartLocal = TimeZoneInfo.ConvertTimeFromUtc(slotStartUtc, hostTimeZone);
    var slotEndLocal = TimeZoneInfo.ConvertTimeFromUtc(slotEndUtc, hostTimeZone);
    var dayOfWeek = slotStartLocal.DayOfWeek;
    var startMinutes = slotStartLocal.Hour * 60 + slotStartLocal.Minute;
    var endMinutes = slotEndLocal.Hour * 60 + slotEndLocal.Minute;

    if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
        startMinutes < 8 * 60 ||
        endMinutes > 18 * 60)
    {
        return Results.BadRequest();
    }

    var buffer = TimeSpan.FromMinutes(15);
    var overlappingBooking = await db.Bookings
        .AsNoTracking()
        .AnyAsync(b => b.HostId == request.HostId &&
                       b.Status == "booked" &&
                       b.StartUtc < slotEndUtc.Add(buffer) &&
                       b.EndUtc > slotStartUtc.Subtract(buffer));

    if (overlappingBooking)
    {
        return Results.Conflict();
    }

    var hasGoogle = app.Services.GetRequiredService<GoogleAuthHelper>().IsConfigured();
    string? googleEventId = null;
    string? googleMeetLink = null;

    if (hasGoogle)
    {
        var googleAuth = app.Services.GetRequiredService<GoogleAuthHelper>();
        var googleOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<GoogleCalendarOptions>>().Value;
        var credential = googleAuth.CreateCredential(host.Email);
        var calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = googleOptions.ApplicationName
        });

        var buffer = TimeSpan.FromMinutes(15);
        var windowStartUtc = slotStartUtc.Subtract(buffer);
        var windowEndUtc = slotEndUtc.Add(buffer);

        var outOfOfficeRequest = calendarService.Events.List(host.CalendarId);
        outOfOfficeRequest.TimeMinDateTimeOffset = windowStartUtc;
        outOfOfficeRequest.TimeMaxDateTimeOffset = windowEndUtc;
        outOfOfficeRequest.SingleEvents = true;
        outOfOfficeRequest.ShowDeleted = false;
        outOfOfficeRequest.MaxResults = 250;

        var outOfOfficeEvents = await outOfOfficeRequest.ExecuteAsync();
        if (outOfOfficeEvents?.Items != null)
        {
            foreach (var calendarEvent in outOfOfficeEvents.Items)
            {
                if (calendarEvent.EventType != "outOfOffice")
                {
                    continue;
                }

                var start = calendarEvent.Start?.DateTimeDateTimeOffset;
                var end = calendarEvent.End?.DateTimeDateTimeOffset;
                if (start.HasValue && end.HasValue)
                {
                    var overlaps = windowStartUtc < end.Value.UtcDateTime &&
                                   windowEndUtc > start.Value.UtcDateTime;
                    if (overlaps)
                    {
                        return Results.Conflict();
                    }
                }
            }
        }

        var calendarEventToCreate = new Event
        {
            Summary = $"Reunion con {request.Client.Name} ({request.Client.Company})",
            Description = $"Email: {request.Client.Email}\nTelefono: {request.Client.Phone}\nMotivo: {request.Client.Reason}",
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = slotStartUtc,
                TimeZone = "UTC"
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = slotEndUtc,
                TimeZone = "UTC"
            },
            Attendees = new List<EventAttendee>
            {
                new EventAttendee { Email = request.Client.Email },
                new EventAttendee { Email = host.Email }
            },
            ConferenceData = new ConferenceData
            {
                CreateRequest = new CreateConferenceRequest
                {
                    RequestId = Guid.NewGuid().ToString()
                }
            }
        };

        var insertRequest = calendarService.Events.Insert(calendarEventToCreate, host.CalendarId);
        insertRequest.ConferenceDataVersion = 1;
        var createdEvent = await insertRequest.ExecuteAsync();
        googleEventId = createdEvent?.Id;
        googleMeetLink = createdEvent?.HangoutLink;
    }

    var booking = new Booking
    {
        HostId = request.HostId,
        DurationMinutes = request.DurationMinutes,
        StartUtc = slotStartUtc,
        EndUtc = slotEndUtc,
        ClientName = request.Client.Name,
        ClientEmail = request.Client.Email,
        ClientCompany = request.Client.Company,
        ClientPhone = request.Client.Phone,
        ClientReason = request.Client.Reason,
        Status = "booked",
        GoogleEventId = googleEventId,
        GoogleMeetLink = googleMeetLink,
        LegalTextId = request.LegalTextId,
        LegalAcceptedAtUtc = request.LegalAcceptedAtUtc.UtcDateTime,
        LegalAcceptedIp = request.LegalAcceptedIp,
        CreatedAtUtc = nowUtc
    };

    var idempotency = new BookingIdempotency
    {
        IdempotencyKey = request.IdempotencyKey,
        Booking = booking,
        CreatedAtUtc = nowUtc,
        ExpiresAtUtc = nowUtc.AddHours(24)
    };

    db.Bookings.Add(booking);
    db.BookingIdempotency.Add(idempotency);
    await db.SaveChangesAsync();

    return Results.Ok(new BookResponse
    {
        BookingId = booking.BookingId,
        GoogleEventId = booking.GoogleEventId ?? string.Empty,
        GoogleMeetLink = booking.GoogleMeetLink ?? string.Empty
    });
})
.WithName("CreateBooking")
.WithOpenApi();

app.Run();
