using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class UpdatesModel
    {
        [Key]
        public int update_id { get; set; }

        [Required]
        public int incident_id { get; set; }

        [Required]
        public int user_id { get; set; }

        [Required]
        public string update_text { get; set; }

        [Required]
        public DateTime? updated_at { get; set; }

        [Required]
        public string token { get; set; }

        public string? attachments { get; set; }
    }
}
