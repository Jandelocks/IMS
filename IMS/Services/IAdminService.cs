using IMS.Models;

namespace IMS.Services
{
    public interface IAdminService
    {
        Task<bool> RestrictUserAsync(int userId, int restrictedBy);
        Task<bool> UnrestrictUserAsync(int userId, int unrestrictedBy);
        Task<bool> DeleteUserAsync(string token, int deletedBy);
        Task<bool> UpdateUserRoleAsync(string token, string role, int updatedBy);
        Task<List<UsersModel>> GetAllUsersExceptAsync(int excludeUserId);
    }
}