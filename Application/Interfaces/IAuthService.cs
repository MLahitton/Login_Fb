using Application.DTOs;
using Applications.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<UserResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<String>>ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<ApiResponse<String>> ResendEmailConfirmationAsync(ResendEmailConfirmationRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<String>>ForgotPasswordAync(ForgotPasswordRequest request);
    Task<ApiResponse<String>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse<String>> LogoutAsync(LogoutRequest request);
    Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string UserId);
}