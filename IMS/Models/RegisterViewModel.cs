using System.ComponentModel.DataAnnotations;

namespace IMS.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        public string full_name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string email { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string department { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("password", ErrorMessage = "Passwords do not match.")]
        public string confirmpassword { get; set; }

        public string role { get; set; }
    }
}
