namespace Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }= DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User {get; set; }= null!;
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;


}