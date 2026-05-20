using  Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get ; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }= DateTime.UtcNow;
}