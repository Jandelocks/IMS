using IMS.Models;
using IMS.Repositories;
using IMS.ViewModels;
using System;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class LoginService : ILoginService
    {
        private readonly ILoginRepository _repository;
        private readonly LogService _logService;

        public LoginService(ILoginRepository repository, LogService logService)
        {
            _repository = repository;
            _logService = logService;
        }

        public async Task<(bool Success, string Message)> RegisterUserAsync(RegisterViewModel model)
        {
            if (_repository.EmailExists(model.email))
                return (false, "Email already exists.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.password);
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var newUser = new UsersModel
            {
                full_name = model.full_name,
                email = model.email,
                password = hashedPassword,
                role = string.IsNullOrEmpty(model.role) ? "user" : model.role,
                created_at = DateTime.Now,
                department = model.department,
                token = token
            };

            await _repository.AddUserAsync(newUser);
            return (true, "User registered successfully.");
        }

        public async Task<(bool Success, string Message)> AdminRegisterUserAsync(RegisterViewModel model)
        {
            if (_repository.EmailExists(model.email))
                return (false, "Email Already Exist");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.password);
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var newUser = new UsersModel
            {
                full_name = model.full_name,
                email = model.email,
                password = hashedPassword,
                role = string.IsNullOrEmpty(model.role) ? "user" : model.role,
                created_at = DateTime.Now,
                department = model.department,
                token = token
            };

            await _repository.AddUserAsync(newUser);
            return (true, "Account added successfully.");
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email, string resetUrlBase)
        {
            var user = _repository.GetUserByEmail(email);
            if (user == null)
                return (false, "Email not found.");

            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.token_forgot = resetToken;
            await _repository.UpdateUserAsync(user);

            var resetLink = $"{resetUrlBase}?token={resetToken}";
            string emailBody = $"Click the link below to reset your password:\n{resetLink}";

            if (!SendEmail(user.email, "Password Reset", emailBody))
                return (false, "Failed to send reset email.");

            return (true, "A reset link has been sent to your email.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
        {
            var user = _repository.GetUserByResetToken(token);
            if (user == null)
                return (false, "Invalid or expired token.");

            user.password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.token_forgot = null;
            await _repository.UpdateUserAsync(user);

            _logService.AddLog(user.user_id, "Update password");
            return (true, "Password reset successful.");
        }

        public bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("jandeleido@gmail.com", "mfsu scrv ymrt qlzl"),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("jandeleido@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }
    }
}
