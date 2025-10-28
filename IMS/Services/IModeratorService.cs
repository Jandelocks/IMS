using IMS.Models;
using Microsoft.AspNetCore.Http;

namespace IMS.Services
{
    public interface IModeratorService
    {
        Task<List<IncidentsModel>> GetUserIncidentsAsync(int userId);
        Task<List<IncidentViewModel>> GetManageIncidentsAsync(int userId);
        Task<List<IncidentViewModel>> GetReviewReportsAsync(int userId);
        Task<bool> ResolveIncidentAsync(int incidentId, int userId, int reporterUserId, string comments, IFormFile attach);
        Task<IncidentStats> GetIncidentStatsAsync(int userId);
    }

    public class IncidentStats
    {
        public int TotalReports { get; set; }
        public int InProgressReports { get; set; }
        public int ResolvedReports { get; set; }
        public int ClosedReports { get; set; }
    }
}