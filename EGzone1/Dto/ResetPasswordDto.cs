using System.ComponentModel.DataAnnotations;

namespace EGZone.DTOs
{
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "New Password Required")]
        [MinLength(6, ErrorMessage = "at least 6 character length")]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "UN-Matched Passwords ! Please Try Again")]
        public string ConfirmPassword { get; set; }
    }
}