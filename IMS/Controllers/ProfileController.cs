using IMS.Data;
using IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Controllers
{
    
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;
        private readonly SessionService _sessionService;
        public ProfileController(ApplicationDbContext context, LogService logService, SessionService sessionService)
        {
            _context = context;
            _logService = logService;
            _sessionService = sessionService;
        }

        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            int userId = _sessionService.GetUserId();
            if (userId == 0)
            {
                return RedirectToAction("Index", "login");
            }

            var profile = await _context.users.FirstOrDefaultAsync(i => i.user_id == userId);
            return View("Index", profile);
        }

        [HttpPost]
        public async Task<IActionResult> Updateprofile(int Id, string name, string email, string password, string confirmpassword, IFormFile profilepic)
        {
            int userId = _sessionService.GetUserId();
            var user = await _context.users.FindAsync(Id);
            if (user == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(password) && password.Length < 8)
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters long.";
                return RedirectToAction("Index");
            }
            if (!string.IsNullOrEmpty(password) && password != confirmpassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction("Index");
            }
            user.full_name = name;
            user.email = email;

            if (!string.IsNullOrEmpty(password))
            {
                user.password = BCrypt.Net.BCrypt.HashPassword(password);
            }

            // Handle profile picture upload
            if (profilepic != null && profilepic.Length > 0)
            {
                // Remove old profile picture if it exists
                if (!string.IsNullOrEmpty(user.profile))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.profile.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new profile picture
                var fileName = $"{Guid.NewGuid()}_{profilepic.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilepic.CopyToAsync(stream);
                }

                user.profile = "/uploads/" + fileName; // Save new path in DB
            }

            _context.users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            _logService.AddLog(userId, "Updated profile");
            return RedirectToAction("Index");
        }
    }
}
