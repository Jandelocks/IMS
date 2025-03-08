using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class CommentsModel
    {
        [Key]
        public int comment_id { get; set; }

        [Required]
        [ForeignKey("Incident")]
        public int incident_id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int user_id { get; set; }

        [Required]
        public string comment_text { get; set; }

        [Required]
        public DateTime commented_at { get; set; }

        [Required]
        public string token { get; set; }

        // Navigation properties
        public virtual IncidentsModel Incident { get; set; }
        public virtual UsersModel User { get; set; }
    }
}
