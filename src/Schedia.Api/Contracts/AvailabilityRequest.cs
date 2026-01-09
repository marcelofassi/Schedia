namespace Schedia.Api.Contracts;

public sealed class AvailabilityRequest
{
    public int HostId { get; set; }
    public DateTimeOffset RangeStart { get; set; }
    public DateTimeOffset RangeEnd { get; set; }
    public int DurationMinutes { get; set; }
}
