using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(80)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Bio { get; set; } = string.Empty;

    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(120)]
    public string StatusMessage { get; set; } = "Activo";
}