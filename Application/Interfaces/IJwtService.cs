using Domain.Entities;

namespace Application.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(ApplicationUser user, IList<string> roles);
    string GenerateFreshToken();
    string HashToken(string token);
}