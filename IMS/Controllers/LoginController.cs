using IMS.Services;
using IMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using reCAPTCHA.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using IMS.Repositories;
using IMS.Models;
using System.Security.Cryptography;

namespace IMS.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly ILoginRepository _repository;
        private readonly IRecaptchaService _recaptchaService;
        private readonly SessionService _sessionService;
        private readonly LogService _logService;
        private readonly ISingleSessionManagerService _sessionManager;

        public LoginController(
            ILoginService loginService,
            ILoginRepository repository,
            IRecaptchaService recaptchaService,
            SessionService sessionService,
            LogService logService,
            ISingleSessionManagerService sessionManager)
        {
            _loginService = loginService;
            _repository = repository;
            _recaptchaService = recaptchaService;
            _sessionService = sessionService;
            _logService = logService;
            _sessionManager = sessionManager;
        }

        public IActionResult Index() => View("login");
        public IActionResult Register() => View("register");
        public IActionResult Forgotpassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var baseUrl = Url.Action("ResetPassword", "Login", null, Request.Scheme);
            var (success, message) = await _loginService.ForgotPasswordAsync(email, baseUrl);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction("Forgotpassword");
        }

        public IActionResult ResetPassword(string token)
        {
            var user = _repository.GetUserByResetToken(token);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired token.";
                return RedirectToAction("Forgotpassword");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            var (success, message) = await _loginService.ResetPasswordAsync(token, newPassword);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(success ? "Index" : "Forgotpassword");
        }

        [HttpPost]
        public async Task<IActionResult> newuser(RegisterViewModel model)
        {
            var (success, message) = await _loginService.RegisterUserAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(success ? "Index" : "Register");
        }

        [HttpPost]
        public async Task<IActionResult> Signup(string email, string password, bool rememberMe = false)
        {
            var recaptchaResponse = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                TempData["ErrorMessage"] = "reCAPTCHA is required.";
                return RedirectToAction("Index");
            }

            var recaptchaResult = await _recaptchaService.Validate(recaptchaResponse);
            if (!recaptchaResult.success)
            {
                TempData["ErrorMessage"] = "reCAPTCHA verification failed.";
                return RedirectToAction("Index");
            }

            var user = _repository.GetUserByEmail(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.password))
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return RedirectToAction("Index");
            }

            if (user.isRistrict)
            {
                TempData["ErrorMessage"] = "You do not have permission to log in.";
                return RedirectToAction("Index");
            }

            // 🔒 Check if user already logged in somewhere else
            var sessionId = HttpContext.Session.Id;
            if (_sessionManager.IsUserAlreadyLoggedIn(user.user_id.ToString(), sessionId))
            {
                TempData["ErrorMessage"] = "You are already logged in from another device. Please log out there first.";
                return RedirectToAction("Index");
            }

            // ✅ Create claims and sign-in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
                new Claim(ClaimTypes.Email, user.email),
                new Claim(ClaimTypes.Role, user.role),
                new Claim("FullName", user.full_name),
                new Claim("Token", user.token)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // 🧠 Capture session info
            var device = Request.Headers["User-Agent"].ToString();
            //var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // ✅ Register this user’s session in DB
            _sessionManager.RegisterUserSession(user.user_id.ToString(), sessionId, device, null);

            // 🧾 Log
            _logService.AddLog(user.user_id, "User logged in");
            TempData["Greeting"] = $"Welcome back, {user.full_name}!";

            // 🚀 Redirect based on role
            return user.role switch
            {
                "admin" => Redirect("/Admin/"),
                "user" => Redirect("/users/dashboard"),
                _ => Redirect("/moderator")
            };
        }

        public async Task<IActionResult> Logout()
        {
            int userId = _sessionService.GetUserId();

            // 🧹 Remove session from DB
            _sessionManager.RemoveUserSession(userId.ToString());

            // 🔐 Rotate token to invalidate existing cookies elsewhere
            var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var user = await _repository.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.token = newToken;
                await _repository.UpdateUserAsync(user);
            }

            // 🚪 Sign out and clear session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("UserToken");
            HttpContext.Session.Clear();

            // 🧾 Log action
            _logService.AddLog(userId, "User logged out");
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            return Redirect("/login");
        }


        public async Task<IActionResult> adminnewuser(RegisterViewModel model)
        {
            var (success, message) = await _loginService.AdminRegisterUserAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction("users", "Admin");
        }

    }
}
