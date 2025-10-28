using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IMS.Controllers
{
    [Authorize(Roles = "user")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly SessionService _sessionService;

        public UsersController(IUserService userService, SessionService sessionService)
        {
            _userService = userService;
            _sessionService = sessionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string token)
        {
            var (department, categories) = await _userService.GetDepartmentAndCategoriesAsync(token);
            if (department == null) return NotFound("Department not found");

            ViewBag.DepartmentId = department.department_id;
            ViewBag.DepartmentName = department.department;
            return View("Index", categories);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReports(string tittle, string description, string priority, string category, List<IFormFile> images, int departmentId)
        {
            await _userService.SubmitReportAsync(_sessionService.GetUserId(), tittle, description, priority, category, images, departmentId);
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Reports() =>
            View("Report", await _userService.GetReportsAsync(_sessionService.GetUserId()));

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteReportAsync(_sessionService.GetUserId(), id);
            return RedirectToAction("Reports");
        }

        public async Task<IActionResult> Edit(string token)
        {
            var vm = await _userService.GetIncidentForEditAsync(token);
            return vm == null ? NotFound("Incident not found") : View("Update", vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInc(int id, string tittle, string description, string category, string priority, List<IFormFile> images, string removedAttachments)
        {
            await _userService.UpdateIncidentAsync(_sessionService.GetUserId(), id, tittle, description, category, priority, images, removedAttachments);
            return RedirectToAction("Reports");
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _sessionService.GetUserId();
            var reports = await _userService.GetDashboardReportsAsync(userId);

            ViewBag.TotalReports = reports.Count;
            ViewBag.PendingReports = reports.Count(i => i.status == "Pending");
            ViewBag.ResolvedReports = reports.Count(i => i.status == "Resolved");
            ViewBag.ClosedReports = reports.Count(i => i.status == "Closed");

            return View("Dashboard", reports);
        }

        public async Task<IActionResult> Resolved(int update_id)
        {
            await _userService.ResolveIncidentAsync(_sessionService.GetUserId(), update_id);
            return RedirectToAction("Reports");
        }

        public async Task<IActionResult> Closed() =>
            View("Resolved", await _userService.GetClosedReportsAsync(_sessionService.GetUserId()));

        public async Task<IActionResult> ReportIncident()
        {
            var departments = await _userService.GetAllDepartmentsAsync();
            return View(departments);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int incidentId, string feedbackText, int rating)
        {
            int userId = _sessionService.GetUserId();
            await _userService.SubmitFeedbackAsync(userId, incidentId, feedbackText, rating);

            TempData["SuccessMessage"] = "Feedback submitted successfully.";
            return RedirectToAction("Closed", "Users");
        }
    }
}