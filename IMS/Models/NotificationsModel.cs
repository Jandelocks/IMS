using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class NotificationsModel
    {
        [Key]
        public int notification_id { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("User")]
        public int? user_id { get; set; }

        public UsersModel User { get; set; }
    }
}
