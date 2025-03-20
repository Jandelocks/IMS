using IMS.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using IMS.Models;
using IMS.ViewModels;
using System.Net.Mail;
using System.Net;
using IMS.Services;
using reCAPTCHA.AspNetCore;
using Org.BouncyCastle.Ocsp;

namespace IMS.Controllers
{
    //[AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly SessionService _sessionService;
        private readonly NotificationService _notificationService;
        public LoginController(ApplicationDbContext context, LogService logService, IRecaptchaService recaptchaService, SessionService sessionService, NotificationService notificationService)
        {
            _context = context;
            _logService = logService;
            _recaptchaService = recaptchaService;
            _sessionService = sessionService;
            _notificationService = notificationService;
        }


        public IActionResult Index()
        {
            return View("login");
        }
        public IActionResult Register()
        {
            return View("register");
        }

        public IActionResult Forgotpassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = _context.users.FirstOrDefault(u => u.email == email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Email not found.";
                return RedirectToAction("Forgotpassword");
            }

            // Generate reset token
            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.token_forgot = resetToken;
            await _context.SaveChangesAsync();

            // Send email with reset link
            var resetLink = Url.Action("ResetPassword", "Login", new { token = resetToken }, Request.Scheme);
            string emailBody = $"Click the link below to reset your password:\n{resetLink}";

            bool emailSent = SendEmail(user.email, "Password Reset", emailBody);
            if (!emailSent)
            {
                TempData["ErrorMessage"] = "Failed to send reset email.";
                return RedirectToAction("Forgotpassword");
            }

            TempData["SuccessMessage"] = "A reset link has been sent to your email.";
            return RedirectToAction("Forgotpassword");
        }

        public IActionResult ResetPassword(string token)
        {
            var user = _context.users.FirstOrDefault(u => u.token_forgot == token);
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
            var user = _context.users.FirstOrDefault(u => u.token_forgot == token);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired token.";
                return RedirectToAction("Forgotpassword");
            }

            user.password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.token_forgot = null; // Clear token after reset
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password reset successful. You can now log in.";
            _logService.AddLog(user.user_id, "Update password");
            return RedirectToAction("Index");
        }

        private bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("jandeleido@gmail.com", "mfsu scrv ymrt qlzl"),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("jandeleido@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        [HttpPost]
        public async Task<IActionResult> newuser(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Register", model); // Return with validation errors
            }

            // Check if email is already taken
            if (_context.users.Any(u => u.email == model.email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View("Register", model);
            }

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.password);

            // Generate a secure token
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Create new user
            var newUser = new UsersModel
            {
                full_name = model.full_name,
                email = model.email,
                password = hashedPassword,
                role = string.IsNullOrEmpty(model.role) ? "user" : model.role,
                created_at = DateTime.Now,
                department = model.department,
                token = token,
                token_forgot = null
            };

            _context.users.Add(newUser);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "You can now Signup";
            return RedirectToAction("");
        }


        [HttpPost]
        public async Task<IActionResult> signup(string email, string password, bool rememberMe = false)
        {
            var recaptchaResponse = Request.Form["g-recaptcha-response"];

            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                TempData["ErrorMessage"] = "reCAPTCHA response is required.";
                return RedirectToAction("index");
            }

            var recaptchaResult = await _recaptchaService.Validate(recaptchaResponse);
            if (!recaptchaResult.success)
            {
                TempData["ErrorMessage"] = "reCAPTCHA verification failed. Please try again.";
                return RedirectToAction("index");
            }

            var user = _context.users.FirstOrDefault(u => u.email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.password))
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return RedirectToAction("index");
            }

            if (user.isRistrict)
            {
                TempData["ErrorMessage"] = "You do not have permission to log in.";
                return RedirectToAction("index");
            }

            // Create Claims for authentication
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
                    new Claim(ClaimTypes.Email, user.email),
                    new Claim(ClaimTypes.Role, user.role),
                    new Claim("FullName", user.full_name),
                    new Claim("Token", user.token) // Store the token in claims
                };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe, // This will keep the login persistent if checked
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.Now.AddHours(1) // 30 days if remember me is checked
            };

            // Sign in user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            HttpContext.Session.SetInt32("UserId", user.user_id);
            HttpContext.Session.SetString("Token", user.token);

            // Save token in a cookie for persistence
            if (rememberMe)
            {
                Response.Cookies.Append("UserToken", user.token, new CookieOptions
                {
                    HttpOnly = true, // Protect from XSS
                    Secure = true, // Use HTTPS
                    Expires = DateTime.UtcNow.AddDays(30) // Expiry of 30 days
                });
            }

            _logService.AddLog(user.user_id, "User logged in");
            await _notificationService.SendNotification(user.user_id, "You have logged in");
            TempData["Greeting"] = $"Welcome back, {user.full_name}!";
            return user.role switch
            {
                "admin" => Redirect("/Admin/"),
                "user" => Redirect("/users/dashboard"),
                _ => Redirect("/moderator") // Default redirect
            };
        }

        public async Task<IActionResult> Logout()
        {
            // Generate a new secure token
            var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Get the currently logged-in user's ID from the session
            int userId = _sessionService.GetUserId();

            if (userId == 0) // Ensure a valid user is logged in
            {
                return Redirect("/login");
            }

            // Find the user in the database
            var user = await _context.users.FindAsync(userId);

            if (user != null)
            {
                user.token = newToken; // Update token
                _context.users.Update(user);
                await _context.SaveChangesAsync(); // Save changes in the database
            }

            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Remove the token from cookies and session
            Response.Cookies.Delete("UserToken");
            HttpContext.Session.Clear();
            _logService.AddLog(userId, "User logged out");
            return Redirect("/login");
        }


        public async Task<IActionResult> adminnewuser(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid Input.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // Check if email is already taken
            if (_context.users.Any(u => u.email == model.email))
            {
                TempData["ErrorMessage"] = "Email Already Exist";
                return RedirectToAction("users", "Admin");
            }

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.password);

            // Generate a secure tok,en
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Create new user
            var newUser = new UsersModel
            {
                full_name = model.full_name,
                email = model.email,
                password = hashedPassword,
                role = string.IsNullOrEmpty(model.role) ? "user" : model.role,
                created_at = DateTime.Now,
                department = model.department,
                token = token,
                token_forgot = null
            };

            _context.users.Add(newUser);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Account added";
            return RedirectToAction("users" , "Admin");
        }
    }
}
