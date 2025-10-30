using IMS.Data;
using IMS.Models;
using Microsoft.EntityFrameworkCore;
namespace IMS.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DepartmentsModel?> GetDepartmentByTokenAsync(string token)
            => await _context.Departments.FirstOrDefaultAsync(d => d.token == token);

        public async Task<List<CategoriesModel>> GetCategoriesByDepartmentIdAsync(int departmentId)
            => await _context.Categories.Where(c => c.department_id == departmentId).ToListAsync();

        public async Task AddIncidentAsync(IncidentsModel incident)
        {
            await _context.Incidents.AddAsync(incident);
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<List<IncidentsModel>> GetUserIncidentsAsync(int userId, bool includeClosed)
        {
            var query = _context.Incidents.Where(i => i.user_id == userId);
            if (!includeClosed)
                query = query.Where(u => u.status != "Closed");
            return await query.ToListAsync();
        }

        public async Task<List<AttachmentsModel>> GetUserAttachmentsAsync(int userId)
            => await _context.Attachments.Where(a => a.user_id == userId).ToListAsync();

        public async Task<List<UpdatesModel>> GetUpdatesByIncidentIdsAsync(List<int> incidentIds)
            => await _context.Updates.Where(u => incidentIds.Contains(u.incident_id)).ToListAsync();

        public async Task<IncidentsModel?> GetIncidentByIdAsync(int id)
            => await _context.Incidents.FindAsync(id);

        public async Task<List<AttachmentsModel>> GetAttachmentsByIncidentIdAsync(int incidentId)
            => await _context.Attachments.Where(a => a.incident_id == incidentId).ToListAsync();

        public async Task<IncidentsModel?> GetIncidentByTokenAsync(string token)
            => await _context.Incidents.FirstOrDefaultAsync(i => i.token == token);

        public async Task<DepartmentsModel?> GetDepartmentByIdAsync(int departmentId)
            => await _context.Departments.FirstOrDefaultAsync(d => d.department_id == departmentId);

        public async Task AddAttachmentAsync(AttachmentsModel attachment)
        {
            await _context.Attachments.AddAsync(attachment);
        }

        public async Task RemoveAttachmentAsync(AttachmentsModel attachment)
        {
            _context.Attachments.Remove(attachment);
        }

        public async Task AddCommentAsync(CommentsModel comment)
        {
            await _context.Comments.AddAsync(comment);
        }

        public async Task<List<CommentsModel>> GetAllCommentsAsync()
            => await _context.Comments.ToListAsync();

        public async Task<UsersModel?> GetUserByIdAsync(int userId)
            => await _context.Users.FindAsync(userId);

        public async Task<UpdatesModel?> GetUpdateByIdAsync(int updateId)
            => await _context.Updates.FindAsync(updateId);

        public async Task<List<DepartmentsModel>> GetAllDepartmentsAsync()
            => await _context.Departments.ToListAsync();
        public async Task RemoveIncidentAsync(int incidentId)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident != null)
            {
                _context.Incidents.Remove(incident);
                await _context.SaveChangesAsync();
            }
        }
    }
}

