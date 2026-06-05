using System.ComponentModel.DataAnnotations;

namespace EGZone.DTOs
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current Password Required ")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Enter New Password")]
        [MinLength(6, ErrorMessage = "New Password must be at least 6 Characters")]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Passwords un-matched , please try again !")]
        public string ConfirmPassword { get; set; }
    }
}