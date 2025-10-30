using IMS.Data;
using IMS.Models;
using System;
using System.Linq;

namespace IMS.Services
{
    public class LogService
    {
        private readonly ApplicationDbContext _context;

        public LogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddLog(int userId, string action)
        {
            var user = _context.Users.FirstOrDefault(u => u.user_id == userId);
            if (user != null)
            {
                var log = new LogsModel
                {
                    user_id = user.user_id,
                    full_name = user.full_name,
                    action = action,
                    log_time = DateTime.Now,
                    token = Guid.NewGuid().ToString()
                };

                _context.Logs.Add(log);
                _context.SaveChanges();
            }
        }
    }
}
