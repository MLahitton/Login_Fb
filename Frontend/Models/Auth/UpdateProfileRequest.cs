namespace Frontend.Models.Auth;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
}
