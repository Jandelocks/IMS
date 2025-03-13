namespace IMS.Models
{
    public class IncidentViewModel
    {
        public List<IncidentsModel> Incidents { get; set; } = new();
        public List<AttachmentsModel> Attachments { get; set; } = new();
        public List<UsersModel> Users { get; set; } = new();
        public UsersModel User { get; set; }
        public IncidentsModel Incident { get; internal set; }
        public List<UpdatesModel> Updates { get; set; } = new();
        public List<CategoriesModel> Categories { get; set; }

        public DepartmentsModel Departments { get; set; }
        public List<DepartmentsModel> Department { get; set; } = new(); 
    }
}
