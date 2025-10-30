using IMS.Data;
using IMS.Models;
using Microsoft.EntityFrameworkCore;

namespace IMS.Repositories
{
    public class ModeratorRepository : IModeratorRepository
    {
        private readonly ApplicationDbContext _context;

        public ModeratorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IncidentsModel>> GetIncidentsByAssignedUserAsync(int userId)
        {
            return await _context.Incidents
                .Where(i => i.assigned_too == userId)
                .ToListAsync();
        }

        public async Task<List<IncidentsModel>> GetIncidentsByAssignedUserAndStatusAsync(int userId, string status)
        {
            return await _context.Incidents
                .Where(i => i.assigned_too == userId && i.status == status)
                .ToListAsync();
        }

        public async Task<List<IncidentsModel>> GetIncidentsByAssignedUserExcludingStatusAsync(int userId, string excludeStatus)
        {
            return await _context.Incidents
                .Where(i => i.assigned_too == userId && i.status != excludeStatus)
                .ToListAsync();
        }

        public async Task<IncidentsModel> GetIncidentByIdAsync(int incidentId)
        {
            return await _context.Incidents.FirstOrDefaultAsync(i => i.incident_id == incidentId);
        }

        public async Task<bool> UpdateIncidentAsync(IncidentsModel incident)
        {
            _context.Incidents.Update(incident);
            return await SaveChangesAsync();
        }

        public async Task<bool> AddUpdateAsync(UpdatesModel update)
        {
            _context.Updates.Add(update);
            return await SaveChangesAsync();
        }

        public async Task<List<UpdatesModel>> GetUpdatesByIncidentIdsAsync(List<int> incidentIds)
        {
            return await _context.Updates
                .Where(u => incidentIds.Contains(u.incident_id))
                .ToListAsync();
        }

        public async Task<List<AttachmentsModel>> GetAllAttachmentsAsync()
        {
            return await _context.Attachments.ToListAsync();
        }

        public async Task<List<UsersModel>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<UsersModel> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<List<CommentsModel>> GetAllCommentsAsync()
        {
            return await _context.Comments.ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}