using IMS.Models;

namespace IMS.Services
{
    public interface ISingleSessionManagerService
    {
        // --- SESSION MANAGEMENT ---
        bool IsUserAlreadyLoggedIn(string userId, string currentSessionId);
        void RegisterUserSession(string userId, string sessionId, string deviceInfo, string ip);
        void RemoveUserSession(string userId);
        string? GetUserSessionId(string userId);
        List<SessionModel> GetActiveSessions();

        // --- PAGE LOCKING ---
        bool TryLockPage(string pageKey, string userId);
        void UnlockPage(string pageKey, string userId);
        bool IsPageLocked(string pageKey, out string? lockedBy);
        Dictionary<string, string> GetActivePageLocks();
    }
}
