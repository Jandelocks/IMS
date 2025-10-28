using IMS.Models;
using IMS.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace IMS.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly LogService _logService;
        private readonly NotificationService _notificationService;
        private readonly IHubContext<IncidentHub> _hubContext;

        public UserService(IUserRepository userRepo, LogService logService, NotificationService notificationService, IHubContext<IncidentHub> hubContext)
        {
            _userRepo = userRepo;
            _logService = logService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        public async Task<(DepartmentsModel?, List<CategoriesModel>)> GetDepartmentAndCategoriesAsync(string token)
        {
            var dept = await _userRepo.GetDepartmentByTokenAsync(token);
            if (dept == null) return (null, new List<CategoriesModel>());
            var cats = await _userRepo.GetCategoriesByDepartmentIdAsync(dept.department_id);
            return (dept, cats);
        }

        public async Task SubmitReportAsync(int userId, string tittle, string description, string priority,
                                            string category, List<IFormFile> images, int departmentId)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadPath);

            var incident = new IncidentsModel
            {
                user_id = userId,
                tittle = tittle,
                description = description,
                priority = priority,
                category = category,
                department_id = departmentId,
                token = token,
                status = "Pending",
                reported_at = DateTime.Now
            };

            await _userRepo.AddIncidentAsync(incident);
            await _userRepo.SaveChangesAsync();

            int incidentId = incident.incident_id;

            if (images != null)
            {
                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                        string filePath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await image.CopyToAsync(stream);

                        await _userRepo.AddAttachmentAsync(new AttachmentsModel
                        {
                            user_id = userId,
                            incident_id = incidentId,
                            file_name = fileName,
                            file_path = "/uploads/" + fileName,
                            uploaded_at = DateTime.Now,
                            token = token
                        });
                    }
                }
                await _userRepo.SaveChangesAsync();
            }

            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user != null)
            {
                await _notificationService.SendNotification(1, $"New incident \"{tittle}\"");
                await _notificationService.SendNotification(userId, $"Thank you, your report is received.");
                await _hubContext.Clients.All.SendAsync("ReceiveIncidentUpdate");
            }
            _logService.AddLog(userId, $"Created an incident: {tittle}");
        }

        public async Task<List<IncidentViewModel>> GetReportsAsync(int userId, bool includeClosed = false)
        {
            var incidents = await _userRepo.GetUserIncidentsAsync(userId, includeClosed);
            var attachments = await _userRepo.GetUserAttachmentsAsync(userId);
            var updates = await _userRepo.GetUpdatesByIncidentIdsAsync(incidents.Select(i => i.incident_id).ToList());

            return incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList()
            }).ToList();
        }

        public async Task DeleteReportAsync(int userId, int incidentId)
        {
            var incident = await _userRepo.GetIncidentByIdAsync(incidentId);
            if (incident == null) return;

            var attachments = await _userRepo.GetAttachmentsByIncidentIdAsync(incidentId);
            foreach (var attachment in attachments)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.file_path.TrimStart('/'));
                if (File.Exists(path)) File.Delete(path);
                await _userRepo.RemoveAttachmentAsync(attachment);
            }

            await _userRepo.RemoveIncidentAsync(incident.incident_id);
            await _userRepo.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveIncidentUpdate");
            _logService.AddLog(userId, "Deleted report");
        }

        public async Task<IncidentViewModel?> GetIncidentForEditAsync(string token)
        {
            var incident = await _userRepo.GetIncidentByTokenAsync(token);
            if (incident == null) return null;

            var department = await _userRepo.GetDepartmentByIdAsync(incident.department_id);
            if (department == null) return null;

            var categories = await _userRepo.GetCategoriesByDepartmentIdAsync(department.department_id);
            var attachments = await _userRepo.GetAttachmentsByIncidentIdAsync(incident.incident_id);

            return new IncidentViewModel
            {
                Attachments = attachments,
                Incident = incident,
                Categories = categories
            };
        }

        public async Task UpdateIncidentAsync(int userId, int incidentId, string tittle, string description,
                                              string category, string priority, List<IFormFile> images, string removedAttachments)
        {
            var incident = await _userRepo.GetIncidentByIdAsync(incidentId);
            if (incident == null) return;

            incident.tittle = tittle;
            incident.description = description;
            incident.category = category;
            incident.priority = priority;

            _logService.AddLog(userId, $"Updated incident: {tittle}");
            await _userRepo.SaveChangesAsync();

            // Remove old attachments
            if (!string.IsNullOrEmpty(removedAttachments))
            {
                var ids = removedAttachments.Split(',').Select(int.Parse).ToList();
                var toRemove = (await _userRepo.GetAttachmentsByIncidentIdAsync(incidentId))
                               .Where(a => ids.Contains(a.attachments_id)).ToList();
                foreach (var a in toRemove)
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", a.file_path.TrimStart('/'));
                    if (File.Exists(filePath)) File.Delete(filePath);
                    await _userRepo.RemoveAttachmentAsync(a);
                }
                await _userRepo.SaveChangesAsync();
            }

            // Add new attachments
            if (images != null)
            {
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploadPath);

                foreach (var img in images)
                {
                    if (img.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                        string filePath = Path.Combine(uploadPath, fileName);
                        using (var s = new FileStream(filePath, FileMode.Create))
                            await img.CopyToAsync(s);

                        await _userRepo.AddAttachmentAsync(new AttachmentsModel
                        {
                            user_id = userId,
                            incident_id = incidentId,
                            file_name = fileName,
                            file_path = "/uploads/" + fileName,
                            uploaded_at = DateTime.Now,
                            token = incident.token
                        });
                    }
                }
                await _userRepo.SaveChangesAsync();
            }

            await _hubContext.Clients.All.SendAsync("ReceiveIncidentUpdate");
        }

        public async Task<List<IncidentsModel>> GetDashboardReportsAsync(int userId)
            => await _userRepo.GetUserIncidentsAsync(userId, includeClosed: true);

        public async Task ResolveIncidentAsync(int userId, int updateId)
        {
            var update = await _userRepo.GetUpdateByIdAsync(updateId);
            if (update == null) return;
            var incident = await _userRepo.GetIncidentByIdAsync(update.incident_id);
            if (incident == null) return;

            incident.status = "Closed";
            incident.updated_at = DateTime.Now;
            await _userRepo.SaveChangesAsync();
            _logService.AddLog(userId, "Incident closed");
        }

        public async Task<List<IncidentViewModel>> GetClosedReportsAsync(int userId)
        {
            var incidents = await _userRepo.GetUserIncidentsAsync(userId, true);
            incidents = incidents.Where(i => i.status == "Closed").ToList();
            var attachments = await _userRepo.GetUserAttachmentsAsync(userId);
            var updates = await _userRepo.GetUpdatesByIncidentIdsAsync(incidents.Select(i => i.incident_id).ToList());
            var comments = await _userRepo.GetAllCommentsAsync();

            return incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Comments = comments.Where(c => c.incident_id == i.incident_id).ToList(),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList(),
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList()
            }).ToList();
        }

        public async Task<List<DepartmentsModel>> GetAllDepartmentsAsync()
        {
            return await _userRepo.GetAllDepartmentsAsync();
        }

        public async Task SubmitFeedbackAsync(int userId, int incidentId, string feedbackText, int rating)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await _userRepo.AddCommentAsync(new CommentsModel
            {
                incident_id = incidentId,
                user_id = userId,
                comment_text = feedbackText,
                rating = rating,
                commented_at = DateTime.Now,
                token = token
            });
            await _userRepo.SaveChangesAsync();
            _logService.AddLog(userId, "Submitted feedback");
        }
    }
}
