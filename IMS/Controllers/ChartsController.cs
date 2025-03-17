using IMS.Data;
using IMS.Models;
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

        public ChartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetIncidentCounts()
        {
            var dailyCount = await _context.incidents
                .GroupBy(i => i.reported_at.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var monthlyCount = await _context.incidents
                .GroupBy(i => new { i.reported_at.Year, i.reported_at.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var yearlyCount = await _context.incidents
                .GroupBy(i => i.reported_at.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(new { daily = dailyCount, monthly = monthlyCount, yearly = yearlyCount });
        }

        // Fetch Incident Data
        [HttpGet]
        public async Task<IActionResult> GetIncidentData(string filter)
        {
            var query = _context.incidents.AsQueryable();

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
            var query = _context.users.AsQueryable();

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

            var dailyCount = await _context.incidents
                .Where(i => i.assigned_too == userId && i.reported_at.Date == today)
                .CountAsync();

            var monthlyCount = await _context.incidents
                .Where(i => i.assigned_too == userId && i.reported_at >= startOfMonth)
                .CountAsync();

            var yearlyCount = await _context.incidents
                .Where(i => i.assigned_too == userId && i.reported_at >= startOfYear)
                .CountAsync();

            return Json(new
            {
                daily = dailyCount,
                monthly = monthlyCount,
                yearly = yearlyCount
            });
        }
    }
}
