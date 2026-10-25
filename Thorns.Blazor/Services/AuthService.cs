using System.Net.Http.Json;
using Thorns.Blazor.Models;

namespace Thorns.Blazor.Services;

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

    public async Task<bool> LoginAsync(
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

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        if (string.IsNullOrWhiteSpace(result?.Token))
        {
            return false;
        }

        await _authStateProvider.SetAuthenticatedUserAsync(result.Token);

        return true;
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.LogoutAsync();
    }
}