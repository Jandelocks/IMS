using IMS.Models;
using IMS.ViewModels;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface ILoginService
    {
        Task<(bool Success, string Message)> RegisterUserAsync(RegisterViewModel model);
        Task<(bool Success, string Message)> AdminRegisterUserAsync(RegisterViewModel model);
        Task<(bool Success, string Message)> ForgotPasswordAsync(string email, string resetUrlBase);
        Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword);
        bool SendEmail(string toEmail, string subject, string body);
    }
}
