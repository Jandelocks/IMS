using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class AttachmentsModel
    {
        [Key]
        public int attachments_id { get; set; }

        [Required]
        [ForeignKey("Incident")]
        public int incident_id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int user_id { get; set; }

        [Required]
        public string file_name { get; set; }

        [Required]
        public string file_path { get; set; }

        [Required]
        public DateTime uploaded_at { get; set; }

        [Required]
        public string token { get; set; }

        // Navigation properties
        public virtual IncidentsModel Incident { get; set; }
        public virtual UsersModel User { get; set; }
    }
}
