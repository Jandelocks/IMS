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

        public LoginController(
            ILoginService loginService,
            ILoginRepository repository,
            IRecaptchaService recaptchaService,
            SessionService sessionService,
            LogService logService)
        {
            _loginService = loginService;
            _repository = repository;
            _recaptchaService = recaptchaService;
            _sessionService = sessionService;
            _logService = logService;
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
        public async Task<IActionResult> signup(string email, string password, bool rememberMe = false)
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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            HttpContext.Session.SetInt32("UserId", user.user_id);
            HttpContext.Session.SetString("Token", user.token);

            if (rememberMe)
            {
                Response.Cookies.Append("UserToken", user.token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddDays(30)
                });
            }

            _logService.AddLog(user.user_id, "User logged in");
            TempData["Greeting"] = $"Welcome back, {user.full_name}!";

            return user.role switch
            {
                "admin" => Redirect("/Admin/"),
                "user" => Redirect("/users/dashboard"),
                _ => Redirect("/moderator")
            };
        }

        public async Task<IActionResult> Logout()
        {
            var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            int userId = _sessionService.GetUserId();

            var user = await _repository.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.token = newToken;
                await _repository.UpdateUserAsync(user);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("UserToken");
            HttpContext.Session.Clear();
            _logService.AddLog(userId, "User logged out");
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
