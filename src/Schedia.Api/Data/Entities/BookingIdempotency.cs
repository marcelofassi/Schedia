namespace Schedia.Api.Data.Entities;

public class BookingIdempotency
{
    public Guid IdempotencyKey { get; set; }
    public long BookingId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public Booking Booking { get; set; } = null!;
}
