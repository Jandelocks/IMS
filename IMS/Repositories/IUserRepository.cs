using IMS.Models;

namespace IMS.Repositories
{
    public interface IUserRepository
    {
        Task<DepartmentsModel?> GetDepartmentByTokenAsync(string token);
        Task<List<CategoriesModel>> GetCategoriesByDepartmentIdAsync(int departmentId);
        Task AddIncidentAsync(IncidentsModel incident);
        Task SaveChangesAsync();
        Task<List<IncidentsModel>> GetUserIncidentsAsync(int userId, bool includeClosed);
        Task<List<AttachmentsModel>> GetUserAttachmentsAsync(int userId);
        Task<List<UpdatesModel>> GetUpdatesByIncidentIdsAsync(List<int> incidentIds);
        Task<IncidentsModel?> GetIncidentByIdAsync(int id);
        Task<List<AttachmentsModel>> GetAttachmentsByIncidentIdAsync(int incidentId);
        Task<IncidentsModel?> GetIncidentByTokenAsync(string token);
        Task<DepartmentsModel?> GetDepartmentByIdAsync(int departmentId);
        Task AddAttachmentAsync(AttachmentsModel attachment);
        Task RemoveAttachmentAsync(AttachmentsModel attachment);
        Task AddCommentAsync(CommentsModel comment);
        Task<List<CommentsModel>> GetAllCommentsAsync();
        Task<UsersModel?> GetUserByIdAsync(int userId);
        Task<UpdatesModel?> GetUpdateByIdAsync(int updateId);
        Task<List<DepartmentsModel>> GetAllDepartmentsAsync();
        Task RemoveIncidentAsync(int incidentId);
    }
}
