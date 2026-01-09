namespace Schedia.Api.Contracts;

public sealed class BookRequest
{
    public int HostId { get; set; }
    public DateTimeOffset SlotStart { get; set; }
    public int DurationMinutes { get; set; }
    public string Lang { get; set; } = "es";
    public ClientInfo Client { get; set; } = new();
    public Guid IdempotencyKey { get; set; }
    public long LegalTextId { get; set; }
    public DateTimeOffset LegalAcceptedAtUtc { get; set; }
    public string? LegalAcceptedIp { get; set; }
}

public sealed class ClientInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Reason { get; set; }
}
