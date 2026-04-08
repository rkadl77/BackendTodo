using System.ComponentModel.DataAnnotations;
using Restoran.Notifications.Models;

namespace Restoran.Notifications.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationPageDto
{
    public List<NotificationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateNotificationDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
}
