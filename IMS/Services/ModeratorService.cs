using IMS.Models;
using IMS.Repositories;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace IMS.Services
{
    public class ModeratorService : IModeratorService
    {
        private readonly IModeratorRepository _moderatorRepository;
        private readonly LogService _logService;
        private readonly NotificationService _notificationService;

        public ModeratorService(
            IModeratorRepository moderatorRepository,
            LogService logService,
            NotificationService notificationService)
        {
            _moderatorRepository = moderatorRepository;
            _logService = logService;
            _notificationService = notificationService;
        }

        public async Task<List<IncidentsModel>> GetUserIncidentsAsync(int userId)
        {
            return await _moderatorRepository.GetIncidentsByAssignedUserAsync(userId);
        }

        public async Task<IncidentStats> GetIncidentStatsAsync(int userId)
        {
            var userReports = await _moderatorRepository.GetIncidentsByAssignedUserAsync(userId);

            return new IncidentStats
            {
                TotalReports = userReports.Count,
                InProgressReports = userReports.Count(i => i.status == "In Progress"),
                ResolvedReports = userReports.Count(i => i.status == "Resolved"),
                ClosedReports = userReports.Count(i => i.status == "Closed")
            };
        }

        public async Task<List<IncidentViewModel>> GetManageIncidentsAsync(int userId)
        {
            var incidents = await _moderatorRepository.GetIncidentsByAssignedUserExcludingStatusAsync(userId, "Closed");

            var incidentIds = incidents.Select(i => i.incident_id).ToList();
            var updates = await _moderatorRepository.GetUpdatesByIncidentIdsAsync(incidentIds);
            var attachments = await _moderatorRepository.GetAllAttachmentsAsync();
            var users = await _moderatorRepository.GetAllUsersAsync();

            var incidentList = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                User = users.FirstOrDefault(u => u.user_id == i.user_id),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList()
            }).ToList();

            return incidentList;
        }

        public async Task<List<IncidentViewModel>> GetReviewReportsAsync(int userId)
        {
            var incidents = await _moderatorRepository.GetIncidentsByAssignedUserAndStatusAsync(userId, "Closed");

            var incidentIds = incidents.Select(i => i.incident_id).ToList();
            var updates = await _moderatorRepository.GetUpdatesByIncidentIdsAsync(incidentIds);
            var attachments = await _moderatorRepository.GetAllAttachmentsAsync();
            var users = await _moderatorRepository.GetAllUsersAsync();
            var comments = await _moderatorRepository.GetAllCommentsAsync();

            var incidentList = incidents.Select(i => new IncidentViewModel
            {
                Incident = i,
                Attachments = attachments.Where(a => a.incident_id == i.incident_id).ToList(),
                User = users.FirstOrDefault(u => u.user_id == i.user_id),
                Updates = updates.Where(u => u.incident_id == i.incident_id).ToList(),
                Comments = comments.Where(c => c.incident_id == i.incident_id).ToList()
            }).ToList();

            return incidentList;
        }

        public async Task<bool> ResolveIncidentAsync(int incidentId, int userId, int reporterUserId, string comments, IFormFile attach)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            string filePath = null;

            // Handle file upload
            if (attach != null && attach.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                filePath = Path.Combine(uploadsFolder, attach.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attach.CopyToAsync(stream);
                }
            }

            // Create update
            var updates = new UpdatesModel
            {
                incident_id = incidentId,
                update_text = comments,
                user_id = reporterUserId,
                token = token,
                updated_at = DateTime.Now,
                attachments = filePath != null ? "/uploads/" + attach.FileName : null
            };

            await _moderatorRepository.AddUpdateAsync(updates);

            // Update incident status
            var incident = await _moderatorRepository.GetIncidentByIdAsync(incidentId);
            if (incident == null)
            {
                return false;
            }

            incident.status = "Resolved";
            await _moderatorRepository.UpdateIncidentAsync(incident);

            // Send notifications
            var user = await _moderatorRepository.GetUserByIdAsync(userId);
            if (user != null)
            {
                await _notificationService.SendNotification(1, $"{user.full_name} has resolved an incident.");
                await _notificationService.SendNotification(userId, "You have successfully resolved an incident.");
                await _notificationService.SendNotification(reporterUserId, "Your reported incident has been resolved.");
            }

            // Log the action
            _logService.AddLog(reporterUserId, $"Resolved an incident: {incident.tittle}");

            return true;
        }
    }
}