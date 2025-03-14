using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        [ForeignKey("Department")]
        public int department_id { get; set; }

        public virtual DepartmentsModel Department { get; set; }
    }
}
