using Schedia.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
    .WithName("Healthcheck")
    .WithOpenApi();

app.MapGet("/api/meta", (int hostId, string? lang) =>
{
    var response = new MetaResponse
    {
        HostId = hostId,
        HostName = "TBD",
        HostRoleTitle = "TBD",
        Lang = string.IsNullOrWhiteSpace(lang) ? "es" : lang,
        LegalTextVersion = "v1",
        LegalTextBody = "TBD"
    };

    return Results.Ok(response);
})
.WithName("GetMeta")
.WithOpenApi();

app.MapPost("/api/availability", (AvailabilityRequest request) =>
{
    var response = new AvailabilityResponse
    {
        Slots = Array.Empty<AvailableSlot>()
    };

    return Results.Ok(response);
})
.WithName("GetAvailability")
.WithOpenApi();

app.MapPost("/api/book", (BookRequest request) =>
{
    var response = new BookResponse
    {
        BookingId = 0,
        GoogleEventId = "TBD",
        GoogleMeetLink = "TBD"
    };

    return Results.Ok(response);
})
.WithName("CreateBooking")
.WithOpenApi();

app.Run();
