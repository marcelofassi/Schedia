namespace Schedia.Api.Data.Entities;

public class NotificationRecipient
{
    public int RecipientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
