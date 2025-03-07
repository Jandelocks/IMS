using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace IMS.Models
{
    public class IMSModel
    {
        
        //public class users
        //{
        //    [Key]
        //    public int user_id { get; set; }

        //    [Required]
        //    public string full_name { get; set; }

        //    [Required]
        //    public string email { get; set; }

        //    [Required]
        //    public string password { get; set; }

        //    [Required]
        //    public string role { get; set; }

        //    [Required]
        //    public string department { get; set; }

        //    [Required]
        //    public DateTime created_at { get; set; }

        //    //[Required]
        //    //public DateTime? updated_at { get; set; }

        //    [Required]
        //    public string token { get; set; }

        //    [Required]
        //    public string token_forgot { get; set; }
        //}

        public class logs
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

        //public class incidents
        //{
        //    [Key]
        //    public int incident_id { get; set; }

        //    [Required]
        //    public int user_id { get; set; }

        //    [Required]
        //    public string tittle { get; set; }

        //    [Required]
        //    public string description { get; set; }

        //    [Required]
        //    public string status { get; set; }

        //    [Required]
        //    public string priority { get; set; }

        //    [Required]
        //    public string category { get; set; }

        //    [Required]
        //    public DateTime reported_at { get; set; }

        //    public int? assigned_too { get; set; }

        //    [Required]
        //    public string token { get; set; }

        //    public DateTime? updated_at { get; set; }
        //}

        //public class updates
        //{
        //    [Key]
        //    public int update_id { get; set; }

        //    [Required]
        //    public int incident_id { get; set; }

        //    [Required]
        //    public int user_id { get; set; }

        //    [Required]
        //    public string update_text { get; set; }

        //    [Required]
        //    public DateTime? updated_at { get; set; }

        //    [Required]
        //    public string token { get; set; }
        //}

        public class commments
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

        //public class categories
        //{
        //    [Key]
        //    public int category_id { get; set; }

        //    [Required]
        //    public string category_name { get; set; }

        //    [Required]
        //    public string decription { get; set; }

        //    [Required]
        //    public string token { get; set; }
        //}

        //public class attachments
        //{
        //    [Key]
        //    public int attachments_id { get; set; }

        //    [Required]
        //    public string file_name { get; set; }

        //    [Required]
        //    public string file_path { get; set; }

        //    [Required]
        //    public DateTime uploaded_at { get; set; }

        //    [Required]
        //    public string token { get; set; }
        //}

    }
}
