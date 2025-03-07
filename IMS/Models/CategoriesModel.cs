using System.ComponentModel.DataAnnotations;

namespace IMS.Models
{
    public class CategoriesModel
    {
        [Key]
        public int category_id { get; set; }

        [Required]
        public string category_name { get; set; }

        [Required]
        public string description { get; set; }

        [Required]
        public string token { get; set; }
    }
}
