using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class AttachmentsModel
    {
        [Key]
        public int attachments_id { get; set; }

        [Required]
        public int incident_id { get; set; }

        [Required]
        public int user_id { get; set; }

        [Required]
        public string file_name { get; set; }

        [Required]
        public string file_path { get; set; }

        [Required]
        public DateTime uploaded_at { get; set; }

        [Required]
        public string token { get; set; }

    }
}
