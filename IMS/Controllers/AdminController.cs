using IMS.Data;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;

namespace IMS.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;
        private readonly SessionService _sessionService;
        private readonly NotificationService _notificationService;
        private readonly IAdminService _adminService;

        public AdminController(
            ApplicationDbContext context,
            LogService logService,
            SessionService sessionService,
            NotificationService notificationService,
            IAdminService adminService)
        {
            _context = context;
            _logService = logService;
            _sessionService = sessionService;
            _notificationService = notificationService;
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            var incident = await _context.incidents.Where(i => i.status == "Pending").OrderByDescending(u => u.incident_id).ToListAsync();
            var incidents = await _context.incidents.ToListAsync();
            ViewBag.TotalReports = incidents.Count;
            ViewBag.PendingReports = incidents.Count(i => i.status == "Pending");
            ViewBag.ResolvedReports = incidents.Count(i => i.status == "Closed");
            ViewBag.InProgressReports = incidents.Count(i => i.status == "In Progress");

            return View("Index", incident);
        }

        public async Task<IActionResult> Incidents()
        {
            var incidents = await _context.incidents.Where(u => u.status != "Closed").ToListAsync();
            var attachments = await _context.attachments.ToListAsync();
            var updates = await _context.updates
                                        .Where(u => incidents.Select(i => i.incident_id)
                                        .Contains(u.incident_id))
                                        .ToListAsync();
            var departments = await _context.departments.ToListAsync();
            var users = await _context.users.ToListAsync();

            var incidentList = incidents.Select(i =>
            {
                var department = departments.FirstOrDefault(d => d.department_id == i.department_id);
                string departmentName = department?.department ?? "Unknown";
                var departmentUsers = users.Where(u => u.department == departmentName && u.role == "moderator").ToList();

                return new IncidentViewModel
                {
                    Incident = i,
                    Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                    Users = departmentUsers,
                    User = users.FirstOrDefault(u => u.user_id == i.assigned_too),
                    Updates = updates.Where(u => u.incident_id == i.incident_id).ToList(),
                    Departments = department
                };
            }).ToList();

            return View("Incident", incidentList);
        }

        public async Task<IActionResult> users()
        {
            int userId = _sessionService.GetUserId();
            var Users = await _adminService.GetAllUsersExceptAsync(userId);
            var Departments = await _context.departments.ToListAsync();

            var viewModel = new IncidentViewModel
            {
                Users = Users,
                Department = Departments
            };
            return View("users", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RestrictUser(int id)
        {
            int userId = _sessionService.GetUserId();
            var result = await _adminService.RestrictUserAsync(id, userId);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction("users");
        }

        [HttpPost]
        public async Task<IActionResult> UnrestrictUser(int id)
        {
            int userId = _sessionService.GetUserId();
            var result = await _adminService.UnrestrictUserAsync(id, userId);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction("users");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int Id)
        {
            int userId = _sessionService.GetUserId();
            var incident = await _context.incidents.FindAsync(Id);

            if (incident == null)
            {
                return NotFound();
            }

            var user = await _context.users.FindAsync(incident.user_id);
            if (user == null)
            {
                return NotFound();
            }

            var attachments = _context.attachments.Where(a => a.incident_id == Id);
            if (attachments.Any())
            {
                _context.attachments.RemoveRange(attachments);
            }

            _context.incidents.Remove(incident);
            _logService.AddLog(userId, $"Remove Incident: {incident.tittle}");
            await _context.SaveChangesAsync();
            await _notificationService.SendNotification(user.user_id, "Your Report was Deleted");
            return RedirectToAction("Incidents");
        }

        [HttpPost]
        public async Task<IActionResult> AssignIncident(int id, int assignedUserId)
        {
            int userId = _sessionService.GetUserId();
            var incident = await _context.incidents.FindAsync(id);
            if (incident == null)
            {
                return NotFound();
            }

            incident.assigned_too = assignedUserId;
            incident.status = "In Progress";
            _context.Update(incident);
            await _context.SaveChangesAsync();
            _logService.AddLog((int)userId, $"Assign Incidents to : {assignedUserId}");
            await _notificationService.SendNotification(assignedUserId, "You have been assigned a new incident");
            return RedirectToAction("Incidents");
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.categories.ToListAsync();
            var departments = await _context.departments.ToListAsync();

            var viewModel = new CategoriesDepartmentsViewModel
            {
                Categories = categories,
                Departments = departments
            };
            return View("Categories", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string category_name, string category_desc, int department)
        {
            int userId = _sessionService.GetUserId();
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var newcategory = new CategoriesModel
            {
                category_name = category_name,
                description = category_desc,
                department_id = department,
                token = token
            };

            _context.categories.Add(newcategory);
            await _context.SaveChangesAsync();

            _logService.AddLog((int)userId, $"Add new category: {category_name}");
            return RedirectToAction("Categories");
        }

        [HttpGet]
        public IActionResult DeleteCategory(int Id)
        {
            int userId = _sessionService.GetUserId();
            var cat = _context.categories.Find(Id);
            if (cat == null)
            {
                return NotFound();
            }
            _context.categories.Remove(cat);
            _context.SaveChanges();

            _logService.AddLog(userId, $"Deleted category: {cat.category_name}");
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string category_name, string category_desc)
        {
            int userId = _sessionService.GetUserId();
            var cat = await _context.categories.FindAsync(id);
            if (cat == null)
            {
                return Content("Invalid Action");
            }

            cat.category_name = category_name;
            cat.description = category_desc;
            await _context.SaveChangesAsync();

            _logService.AddLog((int)userId, $"Update category: {cat.category_name}");
            return RedirectToAction("Categories");
        }

        public async Task<IActionResult> usersLogs()
        {
            var logs = await _context.logs.OrderByDescending(l => l.log_id).ToListAsync();
            return View("logs", logs);
        }

        public async Task<IActionResult> Department()
        {
            var departments = await _context.departments.ToListAsync();
            return View("Department", departments);
        }

        public async Task<IActionResult> DepartmentDetails(string token)
        {
            var department = await _context.departments
                .FirstOrDefaultAsync(d => d.token == token);

            if (department == null)
            {
                return NotFound();
            }

            var users = await _context.users
                .Where(u => u.department == department.department)
                .ToListAsync();

            var categories = await _context.categories.Where(i => i.department_id == department.department_id).ToListAsync();

            var viewModel = new IncidentViewModel
            {
                Users = users,
                Categories = categories,
                Department = new List<DepartmentsModel> { department }
            };

            return View("DepartmentDetails", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(string department_name, string department_desc, IFormFile image)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            int userId = _sessionService.GetUserId();
            string imagePath = null;

            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/departments");
                var filePath = Path.Combine(uploads, image.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                imagePath = "/departments/" + image.FileName;
            }

            var newDepartment = new DepartmentsModel
            {
                department = department_name,
                description = department_desc,
                ImagePath = imagePath,
                token = token
            };

            _context.departments.Add(newDepartment);
            await _context.SaveChangesAsync();
            _logService.AddLog(userId, $"Add new department: {department_name}");
            return RedirectToAction("Department");
        }

        [HttpGet]
        public IActionResult DeleteDepartment(int Id)
        {
            int userId = _sessionService.GetUserId();
            var dep = _context.departments.Find(Id);
            if (dep == null)
            {
                return NotFound();
            }
            _context.departments.Remove(dep);
            _context.SaveChanges();
            _logService.AddLog(userId, $"Deleted department: {dep.department}");
            return RedirectToAction("Department");
        }

        [HttpPost]
        public async Task<IActionResult> EditDepartment(int id, string department_name, string department_desc, IFormFile image)
        {
            int userId = _sessionService.GetUserId();
            var department = await _context.departments.FindAsync(id);

            string imagePath = department.ImagePath;

            if (image != null && image.Length > 0)
            {
                if (!string.IsNullOrEmpty(department.ImagePath))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", department.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/departments");
                var filePath = Path.Combine(uploads, image.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                imagePath = "/departments/" + image.FileName;
            }

            department.department = department_name;
            department.description = department_desc;
            department.ImagePath = imagePath;
            await _context.SaveChangesAsync();
            _logService.AddLog(userId, $"Update department: {department_name}");
            return RedirectToAction("Department");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string token)
        {
            int userId = _sessionService.GetUserId();
            var user = await _context.users.FirstOrDefaultAsync(u => u.token == token);

            if (user == null)
            {
                return NotFound();
            }

            var userName = user.full_name;
            var result = await _adminService.DeleteUserAsync(token, userId);

            if (result)
            {
                TempData["SuccessMessage"] = $"User {userName} has been deleted";
            }

            return RedirectToAction("users");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string token, string role)
        {
            int userId = _sessionService.GetUserId();

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(role))
            {
                return BadRequest();
            }

            var user = await _context.users.FirstOrDefaultAsync(u => u.token == token);
            if (user == null)
            {
                return NotFound();
            }

            var userName = user.full_name;
            var result = await _adminService.UpdateUserRoleAsync(token, role, userId);

            if (result)
            {
                TempData["SuccessMessage"] = $"User {userName} role has been updated to {role}";
            }

            return RedirectToAction("users");
        }

        public async Task<IActionResult> Closed()
        {
            var incidents = await _context.incidents.Where(i => i.status == "Closed").ToListAsync();
            var updates = await _context.updates.ToListAsync();
            var Comments = await _context.comments.ToListAsync();
            var users = await _context.users.ToListAsync();
            var attachments = await _context.attachments.ToListAsync();

            var resolvedlist = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList(),
                Comments = Comments.Where(c => c.incident_id == i.incident_id).ToList(),
                User = users.FirstOrDefault(u => u.user_id == i.user_id),
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList()
            }).ToList();

            return View("Closed", resolvedlist);
        }
    }
}