using IMS.Data;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using System.Security.Claims;
using System.Security.Cryptography;

namespace IMS.Controllers
{
    [Authorize(Roles = "moderator")]
    public class ModeratorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;
        private readonly SessionService _sessionService;
        private readonly NotificationService _notificationService;
        public ModeratorController(ApplicationDbContext context, LogService logService, SessionService sessionService, NotificationService notificationService)
        {
            _context = context;
            _logService = logService;
            _sessionService = sessionService;
            _notificationService = notificationService;
        }
        public async Task<IActionResult> Index()
        {
            int userId = _sessionService.GetUserId();
            var userReports = await _context.incidents.
                                                Where(i => i.assigned_too == userId)
                                                .ToListAsync();

            ViewBag.TotalReports = userReports.Count;
            ViewBag.InProressReport = userReports.Count(i => i.status == "In Progress");
            ViewBag.ResolvedReports = userReports.Count(i => i.status == "Resolved");
            ViewBag.ClosedReports = userReports.Count(i => i.status == "Closed");
            ViewBag.UserId = userId;
            ViewBag.UserDepartment = GetUserDepartment();
            return View("Index", userReports);
        }

        public async Task<IActionResult> manageIncidents()
        {
            int userId = _sessionService.GetUserId();
            ViewBag.UserDepartment = GetUserDepartment();
            var incidents = await _context.incidents
                                          .Where(i => i.assigned_too == userId)
                                          .Where(i => i.status != "Closed")
                                          .ToListAsync();

            var updates = await _context.updates
                                      .Where(u => incidents.Select(i => i.incident_id).Contains(u.incident_id))
                                      .ToListAsync();

            var attachments = await _context.attachments.ToListAsync();

            var users = await _context.users.ToListAsync(); // Fetch all users
            // Combine incidents and only fetch attachments that match the incident_id
            var incidentList = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                User = users.FirstOrDefault(u => u.user_id == i.user_id),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList()
            }).ToList();

            return View("manageIncidents", incidentList);
        }

        [HttpPost]
        public async Task<IActionResult> Resolve(int incidentId, int userid, string comments, IFormFile attach)
        {
            int UserId = _sessionService.GetUserId();
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure token
            string filePath = null;
            if (attach != null && attach.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                filePath = Path.Combine(uploadsFolder, attach.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attach.CopyToAsync(stream);
                }
            }

            // Save update
            var updates = new UpdatesModel
            {
                incident_id = incidentId,
                update_text = comments,
                user_id = userid,
                token = token,
                updated_at = DateTime.Now,
                attachments = filePath != null ? "/uploads/" + attach.FileName : null
            };

            _context.updates.Add(updates);


            await _context.SaveChangesAsync();

            // Find the existing incident
            var incident = await _context.incidents.FirstOrDefaultAsync(i => i.incident_id == incidentId);
            if (incident == null)
            {
                return NotFound();
            }

            // Update status
            incident.status = "Resolved";
            _context.incidents.Update(incident);

            var user = await _context.users.FindAsync(UserId);
            if (user != null)
            {
                await _notificationService.SendNotification(1, $"{user.full_name} has resolved an incident.");
                await _notificationService.SendNotification(UserId, "You have successfully resolved an incident.");
                await _notificationService.SendNotification(userid, "Your reported incident has been resolved.");
            }

            _logService.AddLog(userid, $"Rosolved an incident: {incident.tittle}");
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageIncidents");
        }

        public async Task<IActionResult> reviewReports()
        {
            int userId = _sessionService.GetUserId();
            ViewBag.UserDepartment = GetUserDepartment();
            var incidents = await _context.incidents
                                          .Where(i => i.assigned_too == userId)
                                          .Where(i => i.status == "Closed")
                                          .ToListAsync();

            var updates = await _context.updates
                                       .Where(u => incidents.Select(i => i.incident_id).Contains(u.incident_id))
                                       .ToListAsync();

            var attachments = await _context.attachments.ToListAsync();

            var users = await _context.users.ToListAsync(); // Fetch all users

            var Comments = await _context.comments.ToListAsync();
            // Combine incidents and only fetch attachments that match the incident_id
            var incidentList = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                User = users.FirstOrDefault(u => u.user_id == i.user_id),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList(),
                Comments = Comments.Where(c => c.incident_id == i.incident_id).ToList(),
            }).ToList();

            return View("Reports", incidentList);
        }
        public async Task<IActionResult> Department(string department)
        {
            var departmentUsers = await _context.departments
                .FirstOrDefaultAsync(d => d.department == department);

            if (departmentUsers == null)
            {
                return NotFound();
            }

            var users = await _context.users
                .Where(u => u.department == departmentUsers.department) 
                .ToListAsync();

            var categories = await _context.categories
                .Where(i => i.department_id == departmentUsers.department_id) 
                .ToListAsync();

            var viewModel = new IncidentViewModel
            {
                Users = users,
                Categories = categories,
                Department = new List<DepartmentsModel> { departmentUsers } 
            };

            return View("Department", viewModel);
        }

        private string GetUserDepartment()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user ID
            if (userId == null)
                return "N/A"; // Return "N/A" if user is not logged in

            var userDepartment = _context.users
                .Where(u => u.user_id.ToString() == userId) // Convert user_id to string for comparison
                .Select(u => u.department)
                .FirstOrDefault(); // Fetch department from DB

            return userDepartment ?? "N/A"; // Default to "N/A" if department is null
        }
    }
}
