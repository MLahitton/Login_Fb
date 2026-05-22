using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return ToActionResult(response);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var response = await _authService.ConfirmEmailAsync(request);
        return ToActionResult(response);
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendEmailConfirmationRequest request)
    {
        var response = await _authService.ResendEmailConfirmationAsync(request);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return ToActionResult(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var response = await _authService.ForgotPasswordAsync(request);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var response = await _authService.ResetPasswordAsync(request);
        return ToActionResult(response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        return ToActionResult(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        var response = await _authService.LogoutAsync(request);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Token invalido."));
        }

        var response = await _authService.GetCurrentUserAsync(userId);
        return ToActionResult(response);
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Token invalido."));
        }

        var response = await _authService.GetProfileAsync(userId);
        return ToActionResult(response);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Token invalido."));
        }

        var response = await _authService.UpdateProfileAsync(userId, request);
        return ToActionResult(response);
    }

    [Authorize]
    [HttpPost("profile/photo")]
    public async Task<IActionResult> UploadProfilePhoto([FromForm] IFormFile? file)
    {
        return await UploadImageAsync(file, "profile-photo", isProfilePhoto: true);
    }

    [Authorize]
    [HttpPost("profile/cover")]
    public async Task<IActionResult> UploadCoverPhoto([FromForm] IFormFile? file)
    {
        return await UploadImageAsync(file, "cover-photo", isProfilePhoto: false);
    }

    private IActionResult ToActionResult<T>(ApiResponse<T> response)
    {
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private async Task<IActionResult> UploadImageAsync(IFormFile? file, string prefix, bool isProfilePhoto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Token invalido."));
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Debes seleccionar una imagen."));
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return BadRequest(ApiResponse<string>.Fail("La imagen supera el maximo de 5MB."));
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<string>.Fail("Solo se permiten archivos de imagen."));
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<string>.Fail("Formato no permitido. Usa JPG, PNG o WEBP."));
        }

        var webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(webRootPath);

        var userFolderRelative = Path.Combine("uploads", userId);
        var userFolderAbsolute = Path.Combine(webRootPath, userFolderRelative);
        Directory.CreateDirectory(userFolderAbsolute);

        var safeFileName = $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absoluteFilePath = Path.Combine(userFolderAbsolute, safeFileName);

        await using (var stream = System.IO.File.Create(absoluteFilePath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/{userFolderRelative.Replace("\\", "/")}/{safeFileName}";

        var response = isProfilePhoto
            ? await _authService.UpdateProfileImagesAsync(userId, relativeUrl, null)
            : await _authService.UpdateProfileImagesAsync(userId, null, relativeUrl);

        return ToActionResult(response);
    }
}
