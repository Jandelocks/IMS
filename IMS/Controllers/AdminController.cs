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
        public AdminController(ApplicationDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<IActionResult> Index()
        {
            var incident = await _context.incidents.ToListAsync();

            ViewBag.TotalReports = incident.Count;
            ViewBag.PendingReports = incident.Count(i => i.status == "Pending");
            ViewBag.ResolvedReports = incident.Count(i => i.status == "Resolved");
            ViewBag.InProgressReports = incident.Count(i => i.status == "In Progress");

            return View("Index", incident); // Pass incident directly to the view
        }

        public async Task<IActionResult> Incidents()
        {
            // Fetch all incidents
            var incidents = await _context.incidents.ToListAsync();

            // Fetch attachments related to those incidents
            var attachments = await _context.attachments.ToListAsync();

            // Fetch updates related to those incidents
            var updates = await _context.updates
                                        .Where(u => incidents.Select(i => i.incident_id)
                                        .Contains(u.incident_id))
                                        .ToListAsync();

            // Fetch all departments
            var departments = await _context.departments.ToListAsync();

            // Fetch all users
            var users = await _context.users.ToListAsync();

            // Combine incidents and filter users based on department name
            var incidentList = incidents.Select(i =>
            {
                // Find department name based on incident's department_id
                var department = departments.FirstOrDefault(d => d.department_id == i.department_id);
                string departmentName = department?.department ?? "Unknown";

                // Find users who have the same department name
                var departmentUsers = users.Where(u => u.department == departmentName && u.role == "moderator").ToList();

                return new IncidentViewModel
                {
                    Incident = i,
                    Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                    Users = departmentUsers, // Filtered users based on department name
                    User = users.FirstOrDefault(u => u.user_id == i.assigned_too),
                    Updates = updates.Where(u => u.incident_id == i.incident_id).ToList(),
                    Departments = department
                };
            }).ToList();

            return View("Incident", incidentList);
        }


        public async Task<IActionResult> users()
        {
            var Users = await _context.users.Where(i => i.role != "admin").ToListAsync();
            var Departments = await _context.departments.ToListAsync();

            var viewModel = new IncidentViewModel
            {
                Users = Users,
                Department = Departments
            };
            return View("users", viewModel);
        }

        // POST: Restrict User
        [HttpPost]
        public async Task<IActionResult> RestrictUser(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.isRistrict = true; // Restrict user
            await _context.SaveChangesAsync();
            _logService.AddLog((int)userId, $"Restrict: {id}");
            return RedirectToAction("users");
        }

        [HttpPost]
        public async Task<IActionResult> UnrestrictUser(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.isRistrict = false; // Unrestrict user
            await _context.SaveChangesAsync();
            _logService.AddLog((int)userId, $"Unrestrict: {id}");
            return RedirectToAction("users");
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int Id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var incident = await _context.incidents.FindAsync(Id);
            if (incident == null)
            {
                return NotFound();
            }

            // Find and remove related attachments
            var attachments = _context.attachments.Where(a => a.incident_id == Id);
            if (attachments.Any())
            {
                _context.attachments.RemoveRange(attachments);
            }

            _context.incidents.Remove(incident);
            _logService.AddLog((int)userId, $"Remove Incident: {incident.tittle}");
            await _context.SaveChangesAsync();

            return RedirectToAction("Incidents"); // Redirect back to list
        }

        [HttpPost]
        public async Task<IActionResult> AssignIncident(int id, int assignedUserId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
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
            return RedirectToAction("Incidents");
        }
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.categories.ToListAsync();
            var departments =await  _context.departments.ToListAsync();

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
            int? userId = HttpContext.Session.GetInt32("UserId");
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var newcategory = new CategoriesModel 
            { 
                category_name = category_name, 
                description = category_desc ,
                department_id = department,
                token = token
            };

            _context.categories.Add(newcategory);
            await _context.SaveChangesAsync(); // Save to SQL Server

            _logService.AddLog((int)userId, $"Add new category: {category_name}");
            return RedirectToAction("Categories");
        }

        [HttpGet]
        public IActionResult DeleteCategory(int Id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var cat = _context.categories.Find(Id); 
            if (cat == null)
            {
                return NotFound(); 
            }
            _context.categories.Remove(cat); 
            _context.SaveChanges();

            _logService.AddLog((int)userId, $"Deleted category: {cat.category_name}");
            return RedirectToAction("Categories"); 
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string category_name, string category_desc)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var cat = await _context.categories.FindAsync(id);
            if (cat == null)
            {
                return Content("no");
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
            var department = await _context.departments.ToListAsync();
            return View("Department",department);
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(string department_name, string department_desc, IFormFile image)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            int? userId = HttpContext.Session.GetInt32("UserId");
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
            _logService.AddLog((int)userId, $"Add new department: {department_name}");
            return RedirectToAction("Department");
        }


        [HttpGet]
        public IActionResult DeleteDepartment(int Id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var dep = _context.departments.Find(Id);
            if (dep == null)
            {
                return NotFound();
            }
            _context.departments.Remove(dep);
            _context.SaveChanges();
            _logService.AddLog((int)userId, $"Deleted department: {dep.department}");
            return RedirectToAction("Department");
        }

        [HttpPost]
        public async Task<IActionResult> EditDepartment(int id, string department_name, string department_desc, IFormFile image)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var department = await _context.departments.FindAsync(id);
            if (department == null)
            {
                return Content("no");
            }

            string imagePath = department.ImagePath;

            if (image != null && image.Length > 0)
            {
                // Delete the old image if it exists
                if (!string.IsNullOrEmpty(department.ImagePath))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", department.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save the new image
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
            _logService.AddLog((int)userId, $"Update department: {department_name}");
            return RedirectToAction("Department");
        }
    }
}
