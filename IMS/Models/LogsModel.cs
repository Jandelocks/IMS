using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class LogsModel
    {
        [Key]
        public int log_id { get; set; } // Ensure primary key is defined

        [Required]
        public int user_id { get; set; } // Foreign key for user

        [Required]
        public string full_name { get; set; } // Missing column: full_name

        [Required]
        public string action { get; set; }

        [Required]
        public DateTime log_time { get; set; }

        [Required]
        public string token { get; set; }

        [ForeignKey("user_id")]
        public UsersModel User { get; set; }
    }
}
