using IMS.Data;
using IMS.Models;
using Microsoft.EntityFrameworkCore;

namespace IMS.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UsersModel> GetUserByIdAsync(int id)
        {
            return await _context.users.FindAsync(id);
        }

        public async Task<UsersModel> GetUserByTokenAsync(string token)
        {
            return await _context.users.FirstOrDefaultAsync(u => u.token == token);
        }

        public async Task<List<UsersModel>> GetAllUsersExceptAsync(int excludeUserId)
        {
            return await _context.users.Where(i => i.user_id != excludeUserId).ToListAsync();
        }

        public async Task<bool> UpdateUserAsync(UsersModel user)
        {
            _context.users.Update(user);
            return await SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(UsersModel user)
        {
            _context.users.Remove(user);
            return await SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}