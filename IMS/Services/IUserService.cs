using IMS.Models;
using IMS.ViewModels;

namespace IMS.Services
{
    public interface IUserService
    {
        Task<(DepartmentsModel?, List<CategoriesModel>)> GetDepartmentAndCategoriesAsync(string token);
        Task SubmitReportAsync(int userId, string tittle, string description, string priority,
                               string category, List<IFormFile> images, int departmentId);
        Task<List<IncidentViewModel>> GetReportsAsync(int userId, bool includeClosed = false);
        Task DeleteReportAsync(int userId, int incidentId);
        Task<IncidentViewModel?> GetIncidentForEditAsync(string token);
        Task UpdateIncidentAsync(int userId, int incidentId, string tittle, string description,
                                 string category, string priority, List<IFormFile> images, string removedAttachments);
        Task<List<IncidentsModel>> GetDashboardReportsAsync(int userId);
        Task ResolveIncidentAsync(int userId, int updateId);
        Task<List<IncidentViewModel>> GetClosedReportsAsync(int userId);
        Task<List<DepartmentsModel>> GetAllDepartmentsAsync();
        Task SubmitFeedbackAsync(int userId, int incidentId, string feedbackText, int rating);
    }
}
