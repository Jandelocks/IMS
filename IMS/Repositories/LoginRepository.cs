using IMS.Data;
using IMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class LoginRepository : ILoginRepository
    {
        private readonly ApplicationDbContext _context;

        public LoginRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public UsersModel? GetUserByEmail(string email)
        {
            return _context.users.FirstOrDefault(u => u.email == email);
        }

        public UsersModel? GetUserByResetToken(string token)
        {
            return _context.users.FirstOrDefault(u => u.token_forgot == token);
        }

        public async Task AddUserAsync(UsersModel user)
        {
            await _context.users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(UsersModel user)
        {
            _context.users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<UsersModel?> GetUserByIdAsync(int userId)
        {
            return await _context.users.FindAsync(userId);
        }

        public bool EmailExists(string email)
        {
            return _context.users.Any(u => u.email == email);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
