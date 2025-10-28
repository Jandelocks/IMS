using IMS.Models;

namespace IMS.Repositories
{
    public interface IModeratorRepository
    {
        Task<List<IncidentsModel>> GetIncidentsByAssignedUserAsync(int userId);
        Task<List<IncidentsModel>> GetIncidentsByAssignedUserAndStatusAsync(int userId, string status);
        Task<List<IncidentsModel>> GetIncidentsByAssignedUserExcludingStatusAsync(int userId, string excludeStatus);
        Task<IncidentsModel> GetIncidentByIdAsync(int incidentId);
        Task<bool> UpdateIncidentAsync(IncidentsModel incident);
        Task<bool> AddUpdateAsync(UpdatesModel update);
        Task<List<UpdatesModel>> GetUpdatesByIncidentIdsAsync(List<int> incidentIds);
        Task<List<AttachmentsModel>> GetAllAttachmentsAsync();
        Task<List<UsersModel>> GetAllUsersAsync();
        Task<UsersModel> GetUserByIdAsync(int userId);
        Task<List<CommentsModel>> GetAllCommentsAsync();
        Task<bool> SaveChangesAsync();
    }
}