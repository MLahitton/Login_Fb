using System.Security.Cryptography;
using System.Text;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class AuthService : IAuthService
{
    private const int CodeExpirationMinutes = 10;
    private const int MaxCodeAttempts = 5;
    private const int RefreshTokenDays = 7;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IJwtService jwtService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return ApiResponse<UserResponse>.Fail("Ya existe una cuenta registrada con ese correo.");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            UserName = email,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ApiResponse<UserResponse>.Fail(FormatIdentityErrors(result));
        }

        var code = GenerateSixDigitCode();
        await SaveEmailVerificationCodeAsync(user.Id, code);
        await _emailService.SendVerificationCodeAsync(email, user.FullName, code);

        return ApiResponse<UserResponse>.Ok(
            UserResponse.FromUser(user),
            "Usuario registrado. Revisa tu correo para confirmar la cuenta.");
    }

    public async Task<ApiResponse<string>> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return ApiResponse<string>.Fail("Código inválido o cuenta no encontrada.");
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse<string>.Ok(string.Empty, "El correo ya estaba confirmado.");
        }

        var record = await _context.EmailVerificationCodes
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (record is null)
        {
            return ApiResponse<string>.Fail("Código inválido o vencido.");
        }

        if (record.ExpiresAtUtc < DateTime.UtcNow)
        {
            return ApiResponse<string>.Fail("El código expiró. Solicita uno nuevo.");
        }

        if (record.Attempts >= MaxCodeAttempts)
        {
            return ApiResponse<string>.Fail("Demasiados intentos. Solicita un nuevo código.");
        }

        var expectedHash = HashCode(request.Code, user.Id, "email-confirmation");
        if (record.CodeHash != expectedHash)
        {
            record.Attempts++;
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Fail("Código inválido.");
        }

        user.EmailConfirmed = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ApiResponse<string>.Fail(FormatIdentityErrors(updateResult));
        }

        record.UsedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok(string.Empty, "Correo confirmado correctamente. Ya puedes iniciar sesión.");
    }

    public async Task<ApiResponse<string>> ResendEmailConfirmationAsync(ResendEmailConfirmationRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || user.EmailConfirmed)
        {
            return ApiResponse<string>.Ok(
                string.Empty,
                "Si la cuenta existe y no está confirmada, se enviará un nuevo código.");
        }

        var code = GenerateSixDigitCode();
        await SaveEmailVerificationCodeAsync(user.Id, code);
        await _emailService.SendVerificationCodeAsync(email, user.FullName, code);

        return ApiResponse<string>.Ok(
            string.Empty,
            "Si la cuenta existe y no está confirmada, se enviará un nuevo código.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return ApiResponse<AuthResponse>.Fail("Credenciales inválidas.");
        }

        if (!user.EmailConfirmed)
        {
            return ApiResponse<AuthResponse>.Fail("Debes confirmar tu correo antes de iniciar sesión.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return ApiResponse<AuthResponse>.Fail("La cuenta está bloqueada temporalmente por intentos fallidos.");
        }

        var passwordIsValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordIsValid)
        {
            await _userManager.AccessFailedAsync(user);
            return ApiResponse<AuthResponse>.Fail("Credenciales inválidas.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _jwtService.HashToken(refreshToken);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
        });

        await _context.SaveChangesAsync();

        var authResponse = await BuildAuthResponseAsync(user, refreshToken);

        return ApiResponse<AuthResponse>.Ok(authResponse, "Login exitoso.");
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var genericMessage = "Si el correo existe y está confirmado, se enviará un código de recuperación.";
        var email = NormalizeEmail(request.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !user.EmailConfirmed)
        {
            return ApiResponse<string>.Ok(string.Empty, genericMessage);
        }

        var code = GenerateSixDigitCode();
        await SavePasswordResetCodeAsync(user.Id, code);
        await _emailService.SendPasswordResetCodeAsync(email, user.FullName, code);

        return ApiResponse<string>.Ok(string.Empty, genericMessage);
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return ApiResponse<string>.Fail("Código inválido o usuario no encontrado.");
        }

        var record = await _context.PasswordResetCodes
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (record is null)
        {
            return ApiResponse<string>.Fail("Código inválido o vencido.");
        }

        if (record.ExpiresAtUtc < DateTime.UtcNow)
        {
            return ApiResponse<string>.Fail("El código expiró. Solicita uno nuevo.");
        }

        if (record.Attempts >= MaxCodeAttempts)
        {
            return ApiResponse<string>.Fail("Demasiados intentos. Solicita un nuevo código.");
        }

        var expectedHash = HashCode(request.Code, user.Id, "password-reset");
        if (record.CodeHash != expectedHash)
        {
            record.Attempts++;
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Fail("Código inválido.");
        }

        var identityResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityResetToken, request.NewPassword);

        if (!result.Succeeded)
        {
            return ApiResponse<string>.Fail(FormatIdentityErrors(result));
        }

        record.UsedAtUtc = DateTime.UtcNow;

        var activeRefreshTokens = await _context.RefreshTokens
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in activeRefreshTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok(string.Empty, "Contraseña actualizada correctamente. Inicia sesión con la nueva contraseña.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var incomingTokenHash = _jwtService.HashToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == incomingTokenHash);

        if (storedToken is null || !storedToken.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("Refresh token inválido o expirado.");
        }

        if (!storedToken.User.EmailConfirmed)
        {
            return ApiResponse<AuthResponse>.Fail("La cuenta no está confirmada.");
        }

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtService.HashToken(newRefreshToken);

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
        });

        await _context.SaveChangesAsync();

        var authResponse = await BuildAuthResponseAsync(storedToken.User, newRefreshToken);

        return ApiResponse<AuthResponse>.Ok(authResponse, "Token renovado correctamente.");
    }

    public async Task<ApiResponse<string>> LogoutAsync(LogoutRequest request)
    {
        var tokenHash = _jwtService.HashToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (storedToken is not null && storedToken.RevokedAtUtc is null)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return ApiResponse<string>.Ok(string.Empty, "Sesión cerrada correctamente.");
    }

    public async Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ApiResponse<UserResponse>.Fail("Usuario no encontrado.");
        }

        return ApiResponse<UserResponse>.Ok(UserResponse.FromUser(user), "Usuario autenticado.");
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, string refreshToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            User = UserResponse.FromUser(user)
        };
    }

    private async Task SaveEmailVerificationCodeAsync(string userId, string code)
    {
        var oldCodes = await _context.EmailVerificationCodes
            .Where(x => x.UserId == userId && x.UsedAtUtc == null)
            .ToListAsync();

        foreach (var oldCode in oldCodes)
        {
            oldCode.UsedAtUtc = DateTime.UtcNow;
        }

        _context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            UserId = userId,
            CodeHash = HashCode(code, userId, "email-confirmation"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes)
        });

        await _context.SaveChangesAsync();
    }

    private async Task SavePasswordResetCodeAsync(string userId, string code)
    {
        var oldCodes = await _context.PasswordResetCodes
            .Where(x => x.UserId == userId && x.UsedAtUtc == null)
            .ToListAsync();

        foreach (var oldCode in oldCodes)
        {
            oldCode.UsedAtUtc = DateTime.UtcNow;
        }

        _context.PasswordResetCodes.Add(new PasswordResetCode
        {
            UserId = userId,
            CodeHash = HashCode(code, userId, "password-reset"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes)
        });

        await _context.SaveChangesAsync();
    }

    private static string GenerateSixDigitCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string HashCode(string code, string userId, string purpose)
    {
        var rawValue = $"{purpose}:{userId}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawValue));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join(" | ", result.Errors.Select(x => x.Description));
    }
}