namespace Domain.Entities;

public class EmailVerificationCode
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UsedAtUtc { get; set; }

    public int Attempts { get; set; }

    public ApplicationUser User { get; set; } = null!;
}