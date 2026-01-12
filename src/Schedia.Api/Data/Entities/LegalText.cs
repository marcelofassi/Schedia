namespace Schedia.Api.Data.Entities;

public class LegalText
{
    public long LegalTextId { get; set; }
    public string Lang { get; set; } = string.Empty;
    public string VersionLabel { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
