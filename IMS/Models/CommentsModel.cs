using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class CommentsModel
    {
        [Key]
        public int comment_id { get; set; }

        [Required]
        public int incident_id { get; set; }

        [Required]
        public int user_id { get; set; }

        [Required]
        public string comment_text { get; set; }

        [Required]
        public DateTime commented_at { get; set; }

        [Required]
        public string token { get; set; }
    }
}
