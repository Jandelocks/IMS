using IMS.Models;

namespace IMS.Repositories
{
    public interface ILoginRepository
    {
        UsersModel? GetUserByEmail(string email);
        UsersModel? GetUserByResetToken(string token);
        Task AddUserAsync(UsersModel user);
        Task UpdateUserAsync(UsersModel user);
        Task<UsersModel?> GetUserByIdAsync(int userId);
        Task SaveChangesAsync();
        bool EmailExists(string email);
    }
}
