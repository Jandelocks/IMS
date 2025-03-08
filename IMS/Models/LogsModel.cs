using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class LogsModel
    {
        [Key]
        public int user_id { get; set; }

        [Required]
        public string action { get; set; }

        [Required]
        public DateTime log_time { get; set; }

        [Required]
        public string token { get; set; }
    }
}
