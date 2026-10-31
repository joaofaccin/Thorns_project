using System.Net.Http.Json;
using Thorns.Blazor.Models;

namespace Thorns.Blazor.Services;

public enum AuthOperationResult
{
    Success,
    InvalidCredentials,
    Conflict,
    RateLimited
}

public class AuthService
{
    private readonly HttpClient _httpClient;

    private readonly CustomAuthStateProvider _authStateProvider;

    public AuthService(
        HttpClient httpClient,
        CustomAuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthOperationResult> LoginAsync(
        string email,
        string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/Auth/login",
            request
        );

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return AuthOperationResult.RateLimited;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return AuthOperationResult.InvalidCredentials;
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        if (string.IsNullOrWhiteSpace(result?.Token))
        {
            throw new InvalidDataException(
                "A API retornou uma resposta de login sem token.");
        }

        await _authStateProvider.SetAuthenticatedUserAsync(result.Token);

        return AuthOperationResult.Success;
    }

    public async Task<AuthOperationResult> RegisterAsync(
        UserRegistrationForm request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/Auth/register",
            new
            {
                request.Name,
                request.Email,
                request.Password
            });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return AuthOperationResult.Conflict;

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            return AuthOperationResult.RateLimited;

        response.EnsureSuccessStatusCode();
        return AuthOperationResult.Success;
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.LogoutAsync();
    }
}