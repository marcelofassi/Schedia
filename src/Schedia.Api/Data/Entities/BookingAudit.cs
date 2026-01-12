namespace Schedia.Api.Data.Entities;

public class BookingAudit
{
    public long BookingAuditId { get; set; }
    public long BookingId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Booking Booking { get; set; } = null!;
}
