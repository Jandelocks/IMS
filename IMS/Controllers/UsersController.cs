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
        private readonly NotificationService _notificationService;
        public UsersController(ApplicationDbContext context, LogService logService, SessionService sessionService, NotificationService notificationService)
        {
            _context = context;
            _logService = logService;
            _sessionService = sessionService;
            _notificationService = notificationService;
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
                              string category, List<IFormFile> images, int departmentId)
        {
            int userId = _sessionService.GetUserId();
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure token

            // Ensure the directory exists
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Save report details first
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
            int incidentId = submitreport.incident_id;

            // Process each uploaded image
            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        // Generate unique file name
                        string extension = Path.GetExtension(image.FileName);
                        string fileName = $"{Guid.NewGuid()}{extension}";
                        string filePath = Path.Combine(uploadPath, fileName);

                        // Save file to uploads folder
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        // Save file details in Attachments table
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
                    }
                }
                await _context.SaveChangesAsync(); // Save all attachments in the database
            }

            var user = await _context.users.FindAsync(userId);
            if (user != null)
            {
                await _notificationService.SendNotification(1, $"New incident \"{tittle}\" ");
                await _notificationService.SendNotification(userId, $"Thank You, Your report is received.");
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
            var incident = await _context.incidents.FirstOrDefaultAsync(i => i.token == token);

            if (incident == null)
            {
                return NotFound("Incident not found");
            }
            var department = await _context.departments.FirstOrDefaultAsync(d => d.department_id == incident.department_id);

            if (department == null)
            {
                return NotFound("Department not found");
            }

            var categories = await _context.categories
                .Where(c => c.department_id == department.department_id)
                .ToListAsync();

            var attachments = await _context.attachments
                .Where(a => a.incident_id == incident.incident_id)
                .ToListAsync();

            var viewModel = new IncidentViewModel
            {
                Attachments = attachments,
                Incident = incident,
                Categories = categories
            };

            return View("Update", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Updateinc(int Id, string tittle, string description,
                            string category, string priority, List<IFormFile> images, string removedAttachments)
        {
            int userId = _sessionService.GetUserId();
            var incident = _context.incidents.Find(Id);
            if (incident == null)
            {
                return NotFound();
            }

            // Update incident details
            incident.tittle = tittle;
            incident.description = description;
            incident.category = category;
            incident.priority = priority;

            _logService.AddLog(userId, $"Updated incident: {tittle}");

            await _context.SaveChangesAsync();

            // Handle removed attachments
            if (!string.IsNullOrEmpty(removedAttachments))
            {
                var removedIds = removedAttachments.Split(',').Select(int.Parse).ToList();
                var attachmentsToRemove = _context.attachments.Where(a => removedIds.Contains(a.attachments_id)).ToList();

                foreach (var attachment in attachmentsToRemove)
                {
                    // Delete file from server
                    string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.file_path.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    // Remove from database
                    _context.attachments.Remove(attachment);
                }

                await _context.SaveChangesAsync();
            }

            // Handle multiple image uploads
            if (images != null && images.Count > 0)
            {
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        string extension = Path.GetExtension(image.FileName);
                        string fileName = $"{Guid.NewGuid()}{extension}";
                        string filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        // Save each file as a separate attachment
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
                }

                await _context.SaveChangesAsync();
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

            var comments = await _context.comments
                                        .ToListAsync();

            // Combine incidents, attachments, and updates using ViewModel
            var resolvedlist = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Comments = comments.Where(c => c.incident_id == i.incident_id).ToList(),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList()
            }).ToList();

            return View("Resolved", resolvedlist);
        }

        public async Task<IActionResult> ReportIncident()
        {
            var departments = await _context.departments.ToListAsync();
            return View(departments);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int incidentId, string feedbackText, int rating)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            int userId = _sessionService.GetUserId();

            var feedback = new CommentsModel
            {
                incident_id = incidentId,
                user_id = userId,
                comment_text = feedbackText,
                rating = rating,
                commented_at = DateTime.Now,
                token = token
            };

            _context.comments.Add(feedback);
            await _context.SaveChangesAsync();

            _logService.AddLog(userId, "Submitted feedback");

            TempData["SuccessMessage"] = "Feedback submitted successfully.";
            return RedirectToAction("Closed", "Users");
        }
    }
}
