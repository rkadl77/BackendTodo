namespace Restoran.Notifications.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationStatus
{
    Unread,
    Read
}
