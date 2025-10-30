using IMS.Data;
using IMS.Models;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;

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

        // ✅ Check if user already logged in with another session
        public bool IsUserAlreadyLoggedIn(string userId, string currentSessionId)
        {
            int id = int.Parse(userId);
            var existing = _context.Sessions
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == id);

            return existing != null && existing.SessionId != currentSessionId;
        }

        // ✅ Register or update user session
        public void RegisterUserSession(string userId, string sessionId, string deviceInfo, string ip)
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
                        UserName = _context.Users.Where(u => u.user_id == id).Select(u => u.full_name).FirstOrDefault() ?? "Unknown",
                        SessionId = sessionId,
                        DeviceInfo = deviceInfo,
                        IP = ip,
                        LoginTime = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.SessionId = sessionId;
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

        // ✅ Get user’s session ID
        public string? GetUserSessionId(string userId)
        {
            int id = int.Parse(userId);
            return _context.Sessions.AsNoTracking()
                .FirstOrDefault(s => s.UserId == id)?.SessionId;
        }

        // ✅ Get all active sessions
        public List<SessionModel> GetActiveSessions()
        {
            return _context.Sessions
                .Select(s => new SessionModel
                {
                    UserId = s.UserId,
                    SessionId = s.SessionId,
                    DeviceInfo = s.DeviceInfo,
                    IP = s.IP,
                    LoginTime = s.LoginTime
                })
                .ToList();
        }

        // --- PAGE LOCKING (still in memory to keep things fast) ---
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
                var result = _pageLocks.TryGetValue(pageKey, out lockedBy);
                return result;
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
