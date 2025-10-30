using IMS.Models;

namespace IMS.Services
{
    public interface ISingleSessionManagerService
    {
        // --- TOKEN-BASED SESSION MANAGEMENT ---
        string? GetUserToken(string userId);
        void RegisterUserSession(string userId, string token, string deviceInfo, string ip);
        void RemoveUserSession(string userId);
        SessionModel? GetUserSession(string userId);
        List<SessionModel> GetActiveSessions();

        // --- PAGE LOCKING ---
        bool TryLockPage(string pageKey, string userId);
        void UnlockPage(string pageKey, string userId);
        bool IsPageLocked(string pageKey, out string? lockedBy);
        Dictionary<string, string> GetActivePageLocks();
    }
}