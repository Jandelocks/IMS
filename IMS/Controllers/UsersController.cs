using IMS.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using static IMS.Models.IMSModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using IMS.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
namespace IMS.Controllers

{
    [Authorize(Roles = "user")]
    public class UsersController : Controller
    {

        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> submitreports(string tittle, string description, string priority,
                                      string category, IFormFile image)
        {
            int? Id = HttpContext.Session.GetInt32("UserId");
            var userId = Convert.ToInt32(Id);
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
                token = token,
                status = "Pending",
                reported_at = DateTime.UtcNow
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
                    user_id = (int)userId,
                    incident_id = incidentId, // ✅ Link attachment to the incident
                    file_name = fileName,
                    file_path = "/uploads/" + fileName,
                    uploaded_at = DateTime.UtcNow,
                    token = token
                };

                _context.attachments.Add(attach);
                await _context.SaveChangesAsync(); // Save to database
            }

            return RedirectToAction("dashboard"); // Redirect to Dashboard
        }

        public async Task<IActionResult> Reports()
        {
            string? token = HttpContext.Session.GetString("Token");
            string? userRole = HttpContext.Session.GetString("Role");
            int? userId = HttpContext.Session.GetInt32("UserId"); // Get User ID from session

            // Fetch incidents for the logged-in user
            var incidents = await _context.incidents
                                          .Where(i => i.user_id == userId)
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
            var person = _context.incidents.Find(Id); // Find the person by ID
            if (person == null)
            {
                return NotFound(); // Prevents error if person does not exist
            }

            _context.incidents.Remove(person); // Remove the person
            _context.SaveChanges(); // Save changes

            return RedirectToAction("Reports"); // Redirect back to list
        }

        //Update
        public IActionResult edit(int Id)
        {
            string? token = HttpContext.Session.GetString("Token");
            string? userRole = HttpContext.Session.GetString("Role");

            var inc = _context.incidents.Find(Id);
            return View("Update", inc); // Redirect to Edit View
        }

        [HttpPost]
        public async Task<IActionResult> Updateinc(int Id, string tittle, string description,
                                           string category, string priority, IFormFile image)
        {
            var incident = _context.incidents.Find(Id);
            if (incident == null)
            {
                return NotFound();
            }

            incident.tittle = tittle;
            incident.description = description;
            incident.category = category;
            incident.priority = priority;

            _context.SaveChanges(); // Save updated incident details

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login"); // Redirect if session expired
            }

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
                    existingAttachment.uploaded_at = DateTime.UtcNow;
                }
                else
                {
                    // Add new attachment record
                    var attach = new Models.AttachmentsModel
                    {
                        user_id = (int)userId,
                        incident_id = Id,
                        file_name = fileName,
                        file_path = "/uploads/" + fileName,
                        uploaded_at = DateTime.UtcNow,
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
            string? token = HttpContext.Session.GetString("Token");
            string? userRole = HttpContext.Session.GetString("Role");
            int? userId = HttpContext.Session.GetInt32("UserId");

            // Fetch reports belonging to the logged-in user
            var userReports = await _context.incidents
                                            .Where(i => i.user_id == userId)
                                            .ToListAsync();

            // Store counts in ViewBag
            ViewBag.TotalReports = userReports.Count;
            ViewBag.PendingReports = userReports.Count(i => i.status == "Pending");
            ViewBag.ResolvedReports = userReports.Count(i => i.status == "Resolved");

            return View("Dashboard", userReports); // Pass userReports directly to the view
        }
    }
}
