namespace Schedia.Api.Data.Entities;

public class Host
{
    public int HostId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? RoleTitle { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CalendarId { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
