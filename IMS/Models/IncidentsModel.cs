using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class IncidentsModel
    {
        [Key]
        public int incident_id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int user_id { get; set; }

        [Required]
        public string tittle { get; set; }

        [Required]
        public string description { get; set; }

        [Required]
        public string status { get; set; }

        [Required]
        public string priority { get; set; }

        [Required]
        public string category { get; set; }

        [Required]
        public DateTime reported_at { get; set; }

        public int? assigned_too { get; set; }

        [Required]
        public string token { get; set; }

        public DateTime? updated_at { get; set; }

        // Navigation property
        public virtual UsersModel User { get; set; }
        public virtual ICollection<UpdatesModel> Updates { get; set; } = new List<UpdatesModel>();

    }
}
