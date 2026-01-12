using Microsoft.EntityFrameworkCore;
using Schedia.Api.Contracts;
using Schedia.Api.Data;
using Schedia.Api.Data.Entities;

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
