using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
