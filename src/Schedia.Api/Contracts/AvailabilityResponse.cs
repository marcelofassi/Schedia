namespace Schedia.Api.Contracts;

public sealed class AvailabilityResponse
{
    public IReadOnlyList<AvailableSlot> Slots { get; set; } = Array.Empty<AvailableSlot>();
}

public sealed class AvailableSlot
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}
