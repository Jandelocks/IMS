using IMS.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using IMS.Models;
using IMS.Services;
namespace IMS.Controllers

{
    [Authorize(Roles = "user")]
    public class UsersController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;
        private readonly SessionService _sessionService;
        public UsersController(ApplicationDbContext context, LogService logService, SessionService sessionService)
        {
            _context = context;
            _logService = logService;
            _sessionService = sessionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string token)
        {
            // Find the department using the token
            var department = await _context.departments.FirstOrDefaultAsync(d => d.token == token);

            if (department == null)
            {
                return NotFound("Department not found"); // Handle invalid token
            }

            ViewBag.DepartmentId = department.department_id; // Store department_id
            ViewBag.DepartmentName = department.department;  // Optional: Store department name

            var categories = await _context.categories.Where(c => c.department_id == department.department_id).ToListAsync();
            return View("Index", categories);
        }


        [HttpPost]
        public async Task<IActionResult> submitreports(string tittle, string description, string priority,
                                      string category, IFormFile image, int departmentId)
        {
            int userId = _sessionService.GetUserId();
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure token

            // Validate if an image was uploaded
            string filePath = "";
            string fileName = "";

            if (image != null && image.Length > 0)
            {
                // Ensure the directory exists
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate unique file name
                string extension = Path.GetExtension(image.FileName);
                fileName = $"{Guid.NewGuid()}{extension}";
                filePath = Path.Combine(uploadPath, fileName);

                // Save file to uploads folder
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
            }

            // Save report details
            var submitreport = new Models.IncidentsModel
            {
                user_id = userId,
                tittle = tittle,
                description = description,
                priority = priority,
                category = category,
                department_id = departmentId,
                token = token,
                status = "Pending",
                reported_at = DateTime.Now
            };

            _context.incidents.Add(submitreport);
            await _context.SaveChangesAsync(); // Save to database

            // Get the newly generated incident_id
            int incidentId = submitreport.incident_id; // Ensure this property is auto-incremented in your model

            // Save file details in Attachments table if a file was uploaded
            if (!string.IsNullOrEmpty(fileName))
            {
                var attach = new Models.AttachmentsModel
                {
                    user_id = userId,
                    incident_id = incidentId, // ✅ Link attachment to the incident
                    file_name = fileName,
                    file_path = "/uploads/" + fileName,
                    uploaded_at = DateTime.Now,
                    token = token
                };

                _context.attachments.Add(attach);
                await _context.SaveChangesAsync(); // Save to database
            }

            _logService.AddLog(userId, $"Created an incident: {tittle}");
            return RedirectToAction("dashboard"); // Redirect to Dashboard
        }

        public async Task<IActionResult> Reports()
        {
            int userId = _sessionService.GetUserId();

            // Fetch incidents for the logged-in user
            var incidents = await _context.incidents
                                          .Where(i => i.user_id == userId)
                                          .Where(u => u.status != "Closed")
                                          .ToListAsync();

            // Fetch attachments for the same user
            var attachments = await _context.attachments
                                            .Where(a => a.user_id == userId)
                                            .ToListAsync();

            // Fetch updates related to incidents
            var updates = await _context.updates
                                        .Where(u => incidents.Select(i => i.incident_id).Contains(u.incident_id))
                                        .ToListAsync();

            // Combine incidents, attachments, and updates using ViewModel
            var incidentList = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList()
            }).ToList();

            return View("Report", incidentList);
        }


        [HttpGet]
        public IActionResult delete(int Id)
        {
            int userId = _sessionService.GetUserId();
            var person = _context.incidents.Find(Id); // Find the person by ID
            
            if (person == null)
            {
                return NotFound(); // Prevents error if person does not exist
            }

            _context.incidents.Remove(person); // Remove the person
            _context.SaveChanges(); // Save changes
            _logService.AddLog(userId, "Delete Report");
            return RedirectToAction("Reports"); // Redirect back to list
        }

        //Update
        public async Task<IActionResult> Edit(string token)
        {
            // Step 1: Retrieve the incident using the token
            var incident = await _context.incidents.FirstOrDefaultAsync(i => i.token == token);

            if (incident == null)
            {
                return NotFound("Incident not found");
            }

            // Step 2: Find the department using the incident's department_id
            var department = await _context.departments.FirstOrDefaultAsync(d => d.department_id == incident.department_id);

            if (department == null)
            {
                return NotFound("Department not found");
            }

            // Step 3: Retrieve categories belonging to the department
            var categories = await _context.categories
                .Where(c => c.department_id == department.department_id)
                .ToListAsync();

            // Step 4: Pass data to the ViewModel
            var viewModel = new IncidentViewModel
            {
                Incident = incident,
                Categories = categories
            };

            return View("Update", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Updateinc(int Id, string tittle, string description,
                                           string category, string priority, IFormFile image)
        {
            int userId = _sessionService.GetUserId();
            var incident = _context.incidents.Find(Id);
            if (incident == null)
            {
                return NotFound();
            }

            incident.tittle = tittle;
            incident.description = description;
            incident.category = category;
            incident.priority = priority;

            _logService.AddLog(userId, $"Update incident: {tittle}");

            _context.SaveChanges(); // Save updated incident details

            string filePath = "";
            string fileName = "";

            if (image != null && image.Length > 0)
            {
                // Ensure the directory exists
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate unique file name
                string extension = Path.GetExtension(image.FileName);
                fileName = $"{Guid.NewGuid()}{extension}";
                filePath = Path.Combine(uploadPath, fileName);

                // Save file to uploads folder
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Check if an attachment already exists for this incident
                var existingAttachment = _context.attachments.FirstOrDefault(a => a.incident_id == Id);
                if (existingAttachment != null)
                {
                    // **DELETE OLD IMAGE** from server
                    string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingAttachment.file_path.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    // Update existing attachment
                    existingAttachment.file_name = fileName;
                    existingAttachment.file_path = "/uploads/" + fileName;
                    existingAttachment.uploaded_at = DateTime.Now;
                }
                else
                {
                    // Add new attachment record
                    var attach = new Models.AttachmentsModel
                    {
                        user_id = userId,
                        incident_id = Id,
                        file_name = fileName,
                        file_path = "/uploads/" + fileName,
                        uploaded_at = DateTime.Now,
                        token = incident.token
                    };
                    _context.attachments.Add(attach);
                }
                await _context.SaveChangesAsync(); // Save attachment to database
            }
            
            return RedirectToAction("Reports");
        }

        public async Task<IActionResult> Dashboard()
        {
            int userId = _sessionService.GetUserId();
            var userReports = await _context.incidents
                                            .Where(i => i.user_id == userId)
                                            .ToListAsync();
            // Store counts in ViewBag
            ViewBag.TotalReports = userReports.Count;
            ViewBag.PendingReports = userReports.Count(i => i.status == "Pending");
            ViewBag.ResolvedReports = userReports.Count(i => i.status == "Resolved");
            ViewBag.ClosedReports = userReports.Count(i => i.status == "Closed");
            return View("Dashboard", userReports); // Pass userReports directly to the view
        }

        public async Task<IActionResult> Resolved(int update_id)
        {
            int userId = _sessionService.GetUserId();
            var update = await _context.updates.FindAsync(update_id);
            if (update == null)
            {
                return NotFound();
            }
            var incident = await _context.incidents.FindAsync(update.incident_id);
            if (incident == null)
            {
                return NotFound();
            }
            incident.status = "Closed";
            incident.updated_at = DateTime.Now;
            await _context.SaveChangesAsync();

            _logService.AddLog(userId, "Incident closed");
            return RedirectToAction("Reports");
        }

        public async Task<IActionResult> Closed()
        {
            int userId = _sessionService.GetUserId();

            // Fetch incidents for the logged-in user
            var incidents = await _context.incidents
                                          .Where(i => i.user_id == userId)
                                          .Where(u => u.status == "Closed")
                                          .ToListAsync();

            var updates = await _context.updates
                                       .Where(u => incidents.Select(i => i.incident_id).Contains(u.incident_id))
                                       .ToListAsync();

            // Combine incidents, attachments, and updates using ViewModel
            var resolvedlist = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,               
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList()
            }).ToList();

            return View("Resolved", resolvedlist);
        }

        public async Task<IActionResult> ReportIncident()
        {
            var departments = await _context.departments.ToListAsync();
            return View(departments);
        }
    }
}
