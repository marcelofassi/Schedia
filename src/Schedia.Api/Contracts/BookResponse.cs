namespace Schedia.Api.Contracts;

public sealed class BookResponse
{
    public long BookingId { get; set; }
    public string GoogleEventId { get; set; } = string.Empty;
    public string GoogleMeetLink { get; set; } = string.Empty;
}
