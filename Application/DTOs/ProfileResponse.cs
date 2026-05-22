using Domain.Entities;

namespace Application.DTOs;

public class ProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }

    public string Bio { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public string ProfilePhotoUrl { get; set; } = string.Empty;
    public string CoverPhotoUrl { get; set; } = string.Empty;

    public static ProfileResponse FromUser(ApplicationUser user)
    {
        return new ProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            Bio = user.Bio,
            City = user.City,
            StatusMessage = user.StatusMessage,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            CoverPhotoUrl = user.CoverPhotoUrl
        };
    }
}