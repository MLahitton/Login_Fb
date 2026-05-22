using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.Auth
{
    public class ResetPasswordRequest
    {
        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}