using System.ComponentModel.DataAnnotations;

namespace WebApiInterviewStatus.Models.Auth
{
    public class RegisterDto
    {
        [Required]
        [MinLength(5)]
        public string Username { get; set; } = "";

        [Required]
        [MinLength(8)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "Password must contain uppercase, lowercase, number, and special character."
        )]
        public string Password { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(5)]
        public string Fullname { get; set; } = "";

        [Required]
        [Range(2, int.MaxValue, ErrorMessage = "UserRole must be a valid ID")]
        public int UserRole { get; set; }

    }
}
