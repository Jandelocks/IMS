using IMS.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class UserValidationMiddleware
{
    private readonly RequestDelegate _next;

    public UserValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ApplicationDbContext dbContext)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                var user = dbContext.Users.FirstOrDefault(u => u.user_id.ToString() == userId);

                if (user == null)
                {
                    // Logout user
                    await context.SignOutAsync();
                    context.Session.Clear();

                    // Set TempData message via context.Items
                    context.Items["TempDataMessage"] = "Your account no longer exists. Please log in again.";

                    // Redirect to login page
                    context.Response.Redirect("/Login");
                    return;
                }

                if (user.isRistrict)
                {
                    // Logout user
                    await context.SignOutAsync();
                    context.Session.Clear();

                    // Set TempData message via context.Items
                    context.Items["TempDataMessage"] = "Your account has been restricted. Contact support.";

                    // Redirect to login page
                    context.Response.Redirect("/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}
