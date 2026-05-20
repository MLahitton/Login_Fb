using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}