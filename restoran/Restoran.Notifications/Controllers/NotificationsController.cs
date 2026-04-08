using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restoran.Notifications.Data;
using Restoran.Notifications.DTOs;
using Restoran.Notifications.Hubs;
using Restoran.Notifications.Models;

namespace Restoran.Notifications.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationsDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationsController(NotificationsDbContext db, IHubContext<NotificationHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    /// <summary>Список уведомлений текущего пользователя</summary>
    [HttpGet]
    public async Task<ActionResult<NotificationPageDto>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var query = _db.Notifications.Where(n => n.UserId == userId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                Message = n.Message,
                Status = n.Status,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(new NotificationPageDto { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    /// <summary>Создать уведомление вручную (для тестирования / admin)</summary>
    [HttpPost]
    public async Task<ActionResult<NotificationDto>> Create([FromBody] CreateNotificationDto dto)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            OrderId = dto.OrderId,
            Message = dto.Message
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        var result = new NotificationDto
        {
            Id = notification.Id,
            OrderId = notification.OrderId,
            Message = notification.Message,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt
        };

        await _hubContext.Clients
            .Group($"user_{dto.UserId}")
            .SendAsync("ReceiveNotification", result);

        return Ok(result);
    }

    /// <summary>Отметить уведомление прочитанным</summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification == null) return NotFound();

        notification.Status = NotificationStatus.Read;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Отметить все уведомления прочитанными</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
            .ToListAsync();

        foreach (var n in notifications)
            n.Status = NotificationStatus.Read;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Удалить уведомление</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification == null) return NotFound();
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException());
}
