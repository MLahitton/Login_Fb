using Domain.Entities;

namespace Application.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles);

    string GenerateRefreshToken();

    string HashToken(string token);
}