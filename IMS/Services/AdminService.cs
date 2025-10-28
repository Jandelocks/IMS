using IMS.Models;
using IMS.Repositories;

namespace IMS.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly LogService _logService;

        public AdminService(IAdminRepository adminRepository, LogService logService)
        {
            _adminRepository = adminRepository;
            _logService = logService;
        }

        public async Task<bool> RestrictUserAsync(int userId, int restrictedBy)
        {
            var user = await _adminRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.isRistrict = true;
            var result = await _adminRepository.SaveChangesAsync();

            if (result)
            {
                _logService.AddLog(restrictedBy, $"Restrict: {userId}");
            }

            return result;
        }

        public async Task<bool> UnrestrictUserAsync(int userId, int unrestrictedBy)
        {
            var user = await _adminRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.isRistrict = false;
            var result = await _adminRepository.SaveChangesAsync();

            if (result)
            {
                _logService.AddLog(unrestrictedBy, $"Unrestrict: {userId}");
            }

            return result;
        }

        public async Task<bool> DeleteUserAsync(string token, int deletedBy)
        {
            var user = await _adminRepository.GetUserByTokenAsync(token);
            if (user == null)
            {
                return false;
            }

            var userName = user.full_name;
            var result = await _adminRepository.DeleteUserAsync(user);

            if (result)
            {
                _logService.AddLog(deletedBy, $"Delete user: {userName}");
            }

            return result;
        }

        public async Task<bool> UpdateUserRoleAsync(string token, string role, int updatedBy)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(role))
            {
                return false;
            }

            var user = await _adminRepository.GetUserByTokenAsync(token);
            if (user == null)
            {
                return false;
            }

            var userName = user.full_name;
            user.role = role;
            var result = await _adminRepository.UpdateUserAsync(user);

            if (result)
            {
                _logService.AddLog(updatedBy, $"Update user role: {userName} to {role}");
            }

            return result;
        }

        public async Task<List<UsersModel>> GetAllUsersExceptAsync(int excludeUserId)
        {
            return await _adminRepository.GetAllUsersExceptAsync(excludeUserId);
        }
    }
}