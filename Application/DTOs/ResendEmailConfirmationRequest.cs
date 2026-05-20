using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class ResendEmailConfirmationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}