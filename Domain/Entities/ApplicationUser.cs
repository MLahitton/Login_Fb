using  Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get ; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }= DateTime.UtcNow;
    public string Bio { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = "Activo";
    public string ProfilePhotoUrl { get; set; } = string.Empty;
    public string CoverPhotoUrl { get; set; } = string.Empty;
}