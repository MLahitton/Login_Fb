using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Frontend.Models.Auth;
using Microsoft.JSInterop;

namespace Frontend.Services.Auth;

public class AuthApiService
{
    private const string AccessTokenKey = "auth.access_token";
    private const string RefreshTokenKey = "auth.refresh_token";

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

    private async Task<ApiResponse<T>> SendAuthorizedAsync<T>(
        Func<Task<HttpResponseMessage>> requestFactory,
        bool allowRefreshRetry)
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

    private async Task<ApiResponse<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> requestFactory)
    {
        var response = await requestFactory();
        return await ReadApiResponseAsync<T>(response);
    }

    private async Task<ApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);

        if (payload is not null)
        {
            return payload;
        }

        var raw = await response.Content.ReadAsStringAsync();

        return new ApiResponse<T>
        {
            Success = response.IsSuccessStatusCode,
            Message = string.IsNullOrWhiteSpace(raw)
                ? $"Error HTTP {(int)response.StatusCode}."
                : raw
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
