namespace Schedia.Api.Data.Entities;

public class Booking
{
    public long BookingId { get; set; }
    public int HostId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientCompany { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? ClientReason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? GoogleEventId { get; set; }
    public string? GoogleMeetLink { get; set; }
    public long LegalTextId { get; set; }
    public DateTime LegalAcceptedAtUtc { get; set; }
    public string? LegalAcceptedIp { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Host Host { get; set; } = null!;
    public LegalText LegalText { get; set; } = null!;
    public ICollection<BookingIdempotency> IdempotencyKeys { get; set; } = new List<BookingIdempotency>();
    public ICollection<BookingAudit> Audits { get; set; } = new List<BookingAudit>();
}
