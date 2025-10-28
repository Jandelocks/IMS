using IMS.Models;

namespace IMS.Repositories
{
    public interface IAdminRepository
    {
        Task<UsersModel> GetUserByIdAsync(int id);
        Task<UsersModel> GetUserByTokenAsync(string token);
        Task<List<UsersModel>> GetAllUsersExceptAsync(int excludeUserId);
        Task<bool> UpdateUserAsync(UsersModel user);
        Task<bool> DeleteUserAsync(UsersModel user);
        Task<bool> SaveChangesAsync();
    }
}