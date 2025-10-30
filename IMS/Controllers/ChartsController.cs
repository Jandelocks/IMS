using IMS.Data;
using IMS.Models;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Controllers
{
    public class ChartsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SessionService _sessionService;

        public ChartsController(ApplicationDbContext context, SessionService sessionService)
        {
            _context = context;
            _sessionService = sessionService;
        }

        public async Task<IActionResult> GetIncidentCounts()
        {
            var dailyCount = await _context.Incidents
                .GroupBy(i => i.reported_at.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var monthlyCount = await _context.Incidents
                .GroupBy(i => new { i.reported_at.Year, i.reported_at.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var yearlyCount = await _context.Incidents
                .GroupBy(i => i.reported_at.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(new { daily = dailyCount, monthly = monthlyCount, yearly = yearlyCount });
        }

        // Fetch Incident Data
        [HttpGet]
        public async Task<IActionResult> GetIncidentData(string filter)
        {
            var query = _context.Incidents.AsQueryable();

            if (filter == "daily")
            {
                query = query.Where(i => i.reported_at.Date == DateTime.Today);
            }
            else if (filter == "monthly")
            {
                query = query.Where(i => i.reported_at.Month == DateTime.Now.Month);
            }
            else if (filter == "yearly")
            {
                query = query.Where(i => i.reported_at.Year == DateTime.Now.Year);
            }

            var data = await query
                .GroupBy(i => i.category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(data);
        }

        // Fetch User Role Data
        [HttpGet]
        public async Task<IActionResult> GetUserRoleData(string filter)
        {
            var query = _context.Users.AsQueryable();

            if (filter == "daily")
            {
                query = query.Where(u => u.created_at.Date == DateTime.Today);
            }
            else if (filter == "monthly")
            {
                query = query.Where(u => u.created_at.Month == DateTime.Now.Month);
            }
            else if (filter == "yearly")
            {
                query = query.Where(u => u.created_at.Year == DateTime.Now.Year);
            }

            var data = await query
                .GroupBy(u => u.role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetModeratorIncidentChart(int userId)
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var dailyCount = await _context.Incidents
                .Where(i => i.assigned_too == userId && i.reported_at.Date == today)
                .CountAsync();

            var monthlyCount = await _context.Incidents
                .Where(i => i.assigned_too == userId && i.reported_at >= startOfMonth)
                .CountAsync();

            var yearlyCount = await _context.Incidents
                .Where(i => i.assigned_too == userId && i.reported_at >= startOfYear)
                .CountAsync();

            return Json(new
            {
                daily = dailyCount,
                monthly = monthlyCount,
                yearly = yearlyCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetModeratorDepartmentIncidents()
        {
            int userId = _sessionService.GetUserId(); // Get logged-in user ID
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Get department of the logged-in moderator
            var moderator = await _context.Users.FindAsync(userId);
            if (moderator == null || string.IsNullOrEmpty(moderator.department))
                return Json(new { error = "User not found or has no department assigned." });

            string department = moderator.department;

            var departmentIncidents = await _context.Incidents
                .Where(i => i.Department.department == department && i.assigned_too == userId)
                .GroupBy(i => i.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    Daily = g.Count(i => i.reported_at.Date == today),
                    Monthly = g.Count(i => i.reported_at >= startOfMonth),
                    Yearly = g.Count(i => i.reported_at >= startOfYear),
                    Categories = g.GroupBy(i => i.category)
                        .Select(c => new { Category = c.Key, Count = c.Count() })
                        .ToList()
                })
                .ToListAsync();

            return Json(departmentIncidents);
        }
    }
}
