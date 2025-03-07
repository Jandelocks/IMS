using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class UsersModel
    {
        [Key]
        public int user_id { get; set; }

        [Required]
        public string full_name { get; set; }

        [Required]
        public string email { get; set; }

        [Required]
        public string password { get; set; }

        [Required]
        public string role { get; set; }

        public string department { get; set; }

        [Required]
        public DateTime created_at { get; set; }

        [Required]
        public string token { get; set; }

        public string token_forgot { get; set; }

        public bool isRistrict { get; set; }

        public ICollection<IncidentsModel> Incidents { get; set; }
    }
}
