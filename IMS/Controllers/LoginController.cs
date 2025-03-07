using IMS.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using IMS.Models;
using IMS.ViewModels;

namespace IMS.Controllers
{
    //[AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("login");
        }
        public IActionResult Register()
        {
            return View("register");
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
                role = "user",
                created_at = DateTime.UtcNow,
                department = model.department,
                token = token
            };

            _context.users.Add(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("");
        }


        [HttpPost]
        public async Task<IActionResult> signup(string email, string password)
        {
            var user = _context.users.FirstOrDefault(u => u.email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.password))
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View("Login");
            }

            if (user.isRistrict)
            {
                ViewBag.ErrorMessage = "You do not have permission to log in.";
                return View("Login");
            }

            // Debugging: Print user details
            Console.WriteLine($"Logging in: {user.full_name}, Role: {user.role}");

            // Create Claims for authentication
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
                    new Claim(ClaimTypes.Email, user.email),
                    new Claim(ClaimTypes.Role, user.role),
                    new Claim("FullName", user.full_name)
                };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // Debugging: Log claims before signing in
            foreach (var claim in claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }

            // Sign in user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // Debugging: Verify if user is logged in
            Console.WriteLine("User successfully signed in.");

            HttpContext.Session.SetInt32("UserId", user.user_id);
            HttpContext.Session.SetString("Token", user.token);
            // Redirect based on role
            if (user.isRistrict == false)
            {
                if (user.role == "admin")
                {
                    return Redirect("/Admin/");
                }
                else if (user.role == "user")
                {
                    return Redirect("/users/dashboard");
                }
                else
                {
                    return Redirect("/moderator"); // Default redirect if role is undefined
                }
            }
            else
            {
                ViewBag.ErrorMessage = "You dont have Permission to Login";
                return View("Login");
            }
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }
    }
}
