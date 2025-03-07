using IMS.Data;
using IMS.Models;
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
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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

            // Fetch all users who are moderators
            var moderators = await _context.users
                .Where(u => u.role == "moderator")
                .ToListAsync();

            var users = await _context.users.ToListAsync();

            // Combine incidents and attachments using ViewModel
            var incidentList = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                Users = moderators, // Pass the list of moderators
                User = users.FirstOrDefault(u => u.user_id == i.assigned_too)
            }).ToList();

            return View("Incident", incidentList);
        }


        public async Task<IActionResult> users()
        {
            var Users = await _context.users.Where(i => i.role != "admin").ToListAsync();
            return View("users", Users);
        }

        // POST: Restrict User
        [HttpPost]
        public async Task<IActionResult> RestrictUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.isRistrict = true; // Restrict user
            await _context.SaveChangesAsync();

            return RedirectToAction("users");
        }

        // POST: Unrestrict User
        [HttpPost]
        public async Task<IActionResult> UnrestrictUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.isRistrict = false; // Unrestrict user
            await _context.SaveChangesAsync();

            return RedirectToAction("users");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int Id)
        {
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
            await _context.SaveChangesAsync();

            return RedirectToAction("Incidents"); // Redirect back to list
        }

        [HttpPost]
        public async Task<IActionResult> AssignIncident(int id, int assignedUserId)
        {
            var incident = await _context.incidents.FindAsync(id);
            if (incident == null)
            {
                return NotFound();
            }

            incident.assigned_too = assignedUserId;
            incident.status = "In Progress";
            _context.Update(incident);
            await _context.SaveChangesAsync();

            return RedirectToAction("Incidents");
        }
        public IActionResult Categories()
        {
            var categories = _context.categories.ToList();
            return View("Categories", categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string category_name, string category_desc)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var newcategory = new CategoriesModel 
            { 
                category_name = category_name, 
                description = category_desc ,
                token = token
            };

            _context.categories.Add(newcategory);
            await _context.SaveChangesAsync(); // Save to SQL Server
            return RedirectToAction("Categories");
        }

        [HttpGet]
        public IActionResult DeleteCategory(int Id)
        {
            var cat = _context.categories.Find(Id); 
            if (cat == null)
            {
                return NotFound(); 
            }
            _context.categories.Remove(cat); 
            _context.SaveChanges(); 
            return RedirectToAction("Categories"); 
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string category_name, string category_desc)
        {
            var cat = await _context.categories.FindAsync(id);
            if (cat == null)
            {
                return Content("no");
            }

            cat.category_name = category_name; 
            cat.description = category_desc;
            await _context.SaveChangesAsync();

            return RedirectToAction("Categories");
        }
    }
}
