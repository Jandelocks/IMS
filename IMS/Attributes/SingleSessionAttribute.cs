using IMS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace IMS.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class SingleSessionAttribute : ActionFilterAttribute
    {
        private readonly bool _enforceSingleLogin;
        private readonly string? _pageKey;

        /// <summary>
        /// Enforces single login for a user (default)
        /// </summary>
        public SingleSessionAttribute(bool enforceSingleLogin = true)
        {
            _enforceSingleLogin = enforceSingleLogin;
        }

        /// <summary>
        /// Enforces exclusive page access (only one user can access a page at a time)
        /// </summary>
        public SingleSessionAttribute(string pageKey)
        {
            _pageKey = pageKey;
            _enforceSingleLogin = false;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var sessionService = httpContext.RequestServices.GetRequiredService<ISingleSessionManagerService>();
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                base.OnActionExecuting(context);
                return;
            }

            var sessionId = httpContext.Session.Id;

            // 🔒 Enforce single-login
            if (_enforceSingleLogin)
            {
                var storedSessionId = sessionService.GetUserSessionId(userId);

                // If user’s current session is not the one stored in DB, force logout
                if (string.IsNullOrEmpty(storedSessionId) || storedSessionId != sessionId)
                {
                    // Force logout immediately
                    httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
                    httpContext.Session.Clear();

                    // Redirect to login with message
                    context.Result = new RedirectToActionResult("Index", "SingleSession", new
                    {
                        message = "Your account was logged in from another device or your session expired."
                    });
                    return;
                }
            }

            // 🔐 Page-level locking
            if (!string.IsNullOrEmpty(_pageKey))
            {
                if (!sessionService.TryLockPage(_pageKey, userId))
                {
                    sessionService.IsPageLocked(_pageKey, out var lockedBy);
                    context.Result = new RedirectToActionResult("PageInUse", "Home", new { page = _pageKey });
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // 🔓 Automatically unlock page after completion
            var httpContext = context.HttpContext;
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionService = httpContext.RequestServices.GetRequiredService<ISingleSessionManagerService>();

            if (!string.IsNullOrEmpty(_pageKey))
            {
                sessionService.UnlockPage(_pageKey, userId ?? string.Empty);
            }

            base.OnActionExecuted(context);
        }
    }
}
