using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class RegisterRequest
{
    [Required]
    [MinLength(6)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}