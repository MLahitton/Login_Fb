using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<UserResponse>> RegisterAsync(RegisterRequest request);

    Task<ApiResponse<string>> ConfirmEmailAsync(ConfirmEmailRequest request);

    Task<ApiResponse<string>> ResendEmailConfirmationAsync(ResendEmailConfirmationRequest request);

    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);

    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request);

    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);

    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    Task<ApiResponse<string>> LogoutAsync(LogoutRequest request);

    Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string userId);
}