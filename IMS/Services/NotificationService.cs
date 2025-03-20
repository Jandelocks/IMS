using Microsoft.AspNetCore.SignalR;
using IMS.Models;
using System.Threading.Tasks;
using IMS.Data;

public class NotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ApplicationDbContext _context;

    public NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context)
    {
        _hubContext = hubContext;
        _context = context;
    }

    public async Task SendNotification(int userId, string message)
    {
        var notification = new NotificationsModel
        {
            user_id = userId,
            Message = message,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", message);
    }
}
