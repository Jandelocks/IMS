using IMS.Attributes;
using IMS.Data;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IMS.Controllers
{
    [Authorize(Roles = "moderator")]
    public class ModeratorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SessionService _sessionService;
        private readonly IModeratorService _moderatorService;

        public ModeratorController(
            ApplicationDbContext context,
            SessionService sessionService,
            IModeratorService moderatorService)
        {
            _context = context;
            _sessionService = sessionService;
            _moderatorService = moderatorService;
        }
        [SingleSession]
        public async Task<IActionResult> Index()
        {
            int userId = _sessionService.GetUserId();
            var userReports = await _moderatorService.GetUserIncidentsAsync(userId);
            var stats = await _moderatorService.GetIncidentStatsAsync(userId);

            ViewBag.TotalReports = stats.TotalReports;
            ViewBag.InProressReport = stats.InProgressReports;
            ViewBag.ResolvedReports = stats.ResolvedReports;
            ViewBag.ClosedReports = stats.ClosedReports;
            ViewBag.UserId = userId;
            ViewBag.UserDepartment = GetUserDepartment();

            return View("Index", userReports);
        }

        public async Task<IActionResult> manageIncidents()
        {
            int userId = _sessionService.GetUserId();
            ViewBag.UserDepartment = GetUserDepartment();

            var incidentList = await _moderatorService.GetManageIncidentsAsync(userId);

            return View("manageIncidents", incidentList);
        }

        [HttpPost]
        public async Task<IActionResult> Resolve(int incidentId, int userid, string comments, IFormFile attach)
        {
            int UserId = _sessionService.GetUserId();

            var result = await _moderatorService.ResolveIncidentAsync(incidentId, UserId, userid, comments, attach);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction("ManageIncidents");
        }

        public async Task<IActionResult> reviewReports()
        {
            int userId = _sessionService.GetUserId();
            ViewBag.UserDepartment = GetUserDepartment();

            var incidentList = await _moderatorService.GetReviewReportsAsync(userId);

            return View("Reports", incidentList);
        }

        public async Task<IActionResult> Department(string department)
        {
            var departmentUsers = await _context.Departments
                .FirstOrDefaultAsync(d => d.department == department);

            if (departmentUsers == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .Where(u => u.department == departmentUsers.department)
                .ToListAsync();

            var categories = await _context.Categories
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return "N/A";

            var userDepartment = _context.Users
                .Where(u => u.user_id.ToString() == userId)
                .Select(u => u.department)
                .FirstOrDefault();

            return userDepartment ?? "N/A";
        }
    }
}