using IMS.Data;
using IMS.Models;
using Microsoft.EntityFrameworkCore;

namespace IMS.Services
{
    public class SingleSessionManagerService : ISingleSessionManagerService
    {
        private readonly ApplicationDbContext _context;
        private static readonly object _lock = new();

        public SingleSessionManagerService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get user's active token
        public string? GetUserToken(string userId)
        {
            int id = int.Parse(userId);
            return _context.Sessions
                .AsNoTracking()
                .Where(s => s.UserId == id)
                .Select(s => s.Token)
                .FirstOrDefault();
        }

        // ✅ Get user's session info
        public SessionModel? GetUserSession(string userId)
        {
            int id = int.Parse(userId);
            return _context.Sessions
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == id);
        }

        // ✅ Register or update user session with token
        public void RegisterUserSession(string userId, string token, string deviceInfo, string ip)
        {
            int id = int.Parse(userId);

            lock (_lock)
            {
                var existing = _context.Sessions.FirstOrDefault(s => s.UserId == id);

                if (existing == null)
                {
                    _context.Sessions.Add(new SessionModel
                    {
                        UserId = id,
                        UserName = _context.Users
                            .Where(u => u.user_id == id)
                            .Select(u => u.full_name)
                            .FirstOrDefault() ?? "Unknown",
                        Token = token,
                        DeviceInfo = deviceInfo,
                        IP = ip,
                        LoginTime = DateTime.UtcNow
                    });
                }
                else
                {
                    // Update existing session with new token
                    existing.Token = token;
                    existing.DeviceInfo = deviceInfo;
                    existing.IP = ip;
                    existing.LoginTime = DateTime.UtcNow;
                    _context.Sessions.Update(existing);
                }

                _context.SaveChanges();
            }
        }

        // ✅ Remove session when user logs out
        public void RemoveUserSession(string userId)
        {
            int id = int.Parse(userId);
            var existing = _context.Sessions.FirstOrDefault(s => s.UserId == id);
            if (existing != null)
            {
                _context.Sessions.Remove(existing);
                _context.SaveChanges();
            }
        }

        // ✅ Get all active sessions
        public List<SessionModel> GetActiveSessions()
        {
            return _context.Sessions
                .Select(s => new SessionModel
                {
                    UserId = s.UserId,
                    UserName = s.UserName,
                    Token = s.Token,
                    DeviceInfo = s.DeviceInfo,
                    IP = s.IP,
                    LoginTime = s.LoginTime
                })
                .ToList();
        }

        // --- PAGE LOCKING (in-memory for performance) ---
        private static readonly Dictionary<string, string> _pageLocks = new();

        public bool TryLockPage(string pageKey, string userId)
        {
            lock (_pageLocks)
            {
                if (_pageLocks.TryGetValue(pageKey, out var lockedBy))
                {
                    return lockedBy == userId;
                }

                _pageLocks[pageKey] = userId;
                return true;
            }
        }

        public void UnlockPage(string pageKey, string userId)
        {
            lock (_pageLocks)
            {
                if (_pageLocks.TryGetValue(pageKey, out var lockedBy) && lockedBy == userId)
                    _pageLocks.Remove(pageKey);
            }
        }

        public bool IsPageLocked(string pageKey, out string? lockedBy)
        {
            lock (_pageLocks)
            {
                return _pageLocks.TryGetValue(pageKey, out lockedBy);
            }
        }

        public Dictionary<string, string> GetActivePageLocks()
        {
            lock (_pageLocks)
            {
                return new Dictionary<string, string>(_pageLocks);
            }
        }
    }
}