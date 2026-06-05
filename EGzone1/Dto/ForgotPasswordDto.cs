using System.ComponentModel.DataAnnotations;

namespace EGZone.DTOs 
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email Required")]
        [EmailAddress(ErrorMessage = "Incorrect Email ! ")]
        public string Email { get; set; }
    }
}