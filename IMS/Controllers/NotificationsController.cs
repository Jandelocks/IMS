using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using IMS.Models;
using IMS.Data;
using IMS.Services;

[Route("api/[controller]")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SessionService _sessionService;

    public NotificationController(ApplicationDbContext context, SessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        int userId = _sessionService.GetUserId();
        var notifications = await _context.Notifications
            .Where(n => n.user_id == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.notification_id,
                n.Message,
                CreatedAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") // Ensures proper date format
            })
            .ToListAsync();

        return Ok(notifications);
    }


    [HttpPost("mark-as-read/{id}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Notification marked as read" });
    }

    [HttpPost("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        int userId = _sessionService.GetUserId();
        var notifications = await _context.Notifications
            .Where(n => n.user_id == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "All notifications marked as read" });
    }
}
