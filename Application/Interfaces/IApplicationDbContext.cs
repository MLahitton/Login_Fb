using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<PasswordResetCode> PasswordResetCodes { get; }

    DbSet<EmailVerificationCode> EmailVerificationCodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}