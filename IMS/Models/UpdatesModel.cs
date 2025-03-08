using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class UpdatesModel
    {
        [Key]
        public int update_id { get; set; }

        [Required]
        [ForeignKey("Incident")]
        public int incident_id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int user_id { get; set; }

        [Required]
        public string update_text { get; set; }

        [Required]
        public DateTime? updated_at { get; set; }

        [Required]
        public string token { get; set; }

        public string? attachments { get; set; }

        // Navigation properties
        public virtual IncidentsModel Incident { get; set; }
        public virtual UsersModel User { get; set; }
    }
}
