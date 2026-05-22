using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Frontend.Models.Auth;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Frontend.Services.Auth;

public class AuthApiService
{
    private const string AccessTokenKey = "auth.access_token";
    private const string RefreshTokenKey = "auth.refresh_token";
    private const long MaxImageUploadBytes = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public AuthApiService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public Task<ApiResponse<UserResponse>> Register(RegisterRequest request)
    {
        return PostAsync<UserResponse>("register", request);
    }

    public Task<ApiResponse<string>> VerifyCode(VerifyCodeRequest request)
    {
        return PostAsync<string>("confirm-email", request);
    }

    public async Task<ApiResponse<AuthResponse>> Login(LoginRequest request)
    {
        var response = await PostAsync<AuthResponse>("login", request);

        if (response.Success && response.Data is not null)
        {
            await SaveTokensAsync(response.Data.AccessToken, response.Data.RefreshToken);
        }

        return response;
    }

    public Task<ApiResponse<string>> ForgotPassword(ForgotPasswordRequest request)
    {
        return PostAsync<string>("forgot-password", request);
    }

    public Task<ApiResponse<string>> ResetPassword(ResetPasswordRequest request)
    {
        return PostAsync<string>(
            "reset-password",
            new
            {
                request.Email,
                request.Code,
                request.NewPassword
            });
    }

    public Task<ApiResponse<UserResponse>> GetMe()
    {
        return GetAuthorizedAsync<UserResponse>("me");
    }

    public Task<ApiResponse<ProfileResponse>> GetProfile()
    {
        return GetAuthorizedAsync<ProfileResponse>("profile");
    }

    public Task<ApiResponse<ProfileResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        return PutAuthorizedAsync<ProfileResponse>(
            "profile",
            request,
            allowRefreshRetry: true);
    }

    public Task<ApiResponse<ProfileResponse>> UploadProfilePhoto(IBrowserFile file)
    {
        return UploadImageAuthorizedAsync("profile/photo", file);
    }

    public Task<ApiResponse<ProfileResponse>> UploadCoverPhoto(IBrowserFile file)
    {
        return UploadImageAuthorizedAsync("profile/cover", file);
    }

    public async Task<ApiResponse<string>> Logout()
    {
        var refreshToken = await GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await ClearTokensAsync();
            return new ApiResponse<string>
            {
                Success = true,
                Message = "Sesion local cerrada."
            };
        }

        var response = await PostAuthorizedAsync<string>(
            "logout",
            new LogoutRequest
            {
                RefreshToken = refreshToken
            },
            allowRefreshRetry: false);

        await ClearTokensAsync();
        return response;
    }

    public async Task<bool> IsAuthenticated()
    {
        var accessToken = await GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return true;
        }

        return await TryRefreshTokenAsync();
    }

    public string ResolveAssetUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return string.Empty;
        }

        if (_http.BaseAddress is null)
        {
            return rawUrl;
        }

        var apiBase = _http.BaseAddress;
        var apiOrigin = apiBase.GetLeftPart(UriPartial.Authority);

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var absoluteUri))
        {
            // Normaliza enlaces viejos (por ejemplo https:5001) al origen actual del API.
            var sameHost = string.Equals(absoluteUri.Host, apiBase.Host, StringComparison.OrdinalIgnoreCase) ||
                           (absoluteUri.IsLoopback && apiBase.IsLoopback);

            if (sameHost &&
                (absoluteUri.Port != apiBase.Port ||
                 !string.Equals(absoluteUri.Scheme, apiBase.Scheme, StringComparison.OrdinalIgnoreCase)))
            {
                return apiOrigin + absoluteUri.PathAndQuery;
            }

            return absoluteUri.ToString();
        }

        var normalized = rawUrl.StartsWith('/')
            ? rawUrl
            : "/" + rawUrl;

        return apiOrigin + normalized;
    }

    private Task<ApiResponse<T>> PostAsync<T>(string endpoint, object payload)
    {
        return SendAsync<T>(() => _http.PostAsJsonAsync(endpoint, payload));
    }

    private Task<ApiResponse<T>> GetAuthorizedAsync<T>(string endpoint)
    {
        return SendAuthorizedAsync<T>(() => _http.GetAsync(endpoint), allowRefreshRetry: true);
    }

    private Task<ApiResponse<T>> PostAuthorizedAsync<T>(string endpoint, object payload, bool allowRefreshRetry)
    {
        return SendAuthorizedAsync<T>(
            () => _http.PostAsJsonAsync(endpoint, payload),
            allowRefreshRetry);
    }

    private Task<ApiResponse<T>> PutAuthorizedAsync<T>(string endpoint, object payload, bool allowRefreshRetry)
    {
        return SendAuthorizedAsync<T>(
            () => _http.PutAsJsonAsync(endpoint, payload),
            allowRefreshRetry);
    }

    private async Task<ApiResponse<T>> SendAuthorizedAsync<T>(
        Func<Task<HttpResponseMessage>> requestFactory,
        bool allowRefreshRetry)
    {
        try
        {
            await AttachAccessTokenAsync();

            var response = await requestFactory();

            if (allowRefreshRetry && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await AttachAccessTokenAsync();
                    response = await requestFactory();
                }
            }

            var result = await ReadApiResponseAsync<T>(response);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await ClearTokensAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            return CreateNetworkErrorResponse<T>(ex);
        }
    }

    private async Task<ApiResponse<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> requestFactory)
    {
        try
        {
            var response = await requestFactory();
            return await ReadApiResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return CreateNetworkErrorResponse<T>(ex);
        }
    }

    private async Task<ApiResponse<ProfileResponse>> UploadImageAuthorizedAsync(string endpoint, IBrowserFile file)
    {
        try
        {
            await AttachAccessTokenAsync();

            var response = await SendFileRequestAsync(endpoint, file);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await AttachAccessTokenAsync();
                    response = await SendFileRequestAsync(endpoint, file);
                }
            }

            var result = await ReadApiResponseAsync<ProfileResponse>(response);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await ClearTokensAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            return CreateNetworkErrorResponse<ProfileResponse>(ex);
        }
    }

    private async Task<HttpResponseMessage> SendFileRequestAsync(string endpoint, IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(MaxImageUploadBytes);
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        }

        content.Add(streamContent, "file", file.Name);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        return await _http.SendAsync(request);
    }

    private async Task<ApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        var raw = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
                if (payload is not null)
                {
                    return payload;
                }
            }
            catch
            {
                // Si no es JSON esperado, devolvemos mensaje controlado abajo.
            }
        }

        return new ApiResponse<T>
        {
            Success = response.IsSuccessStatusCode,
            Message = string.IsNullOrWhiteSpace(raw)
                ? $"Error HTTP {(int)response.StatusCode}."
                : raw
        };
    }

    private static ApiResponse<T> CreateNetworkErrorResponse<T>(Exception ex)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = $"No se pudo conectar con el API: {ex.Message}"
        };
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        var refreshToken = await GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var response = await PostAsync<AuthResponse>(
            "refresh-token",
            new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            });

        if (!response.Success || response.Data is null)
        {
            await ClearTokensAsync();
            return false;
        }

        await SaveTokensAsync(response.Data.AccessToken, response.Data.RefreshToken);
        return true;
    }

    private async Task AttachAccessTokenAsync()
    {
        var accessToken = await GetAccessTokenAsync();

        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
    }

    private async Task<string?> GetRefreshTokenAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
    }

    private async Task ClearTokensAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        _http.DefaultRequestHeaders.Authorization = null;
    }
}
