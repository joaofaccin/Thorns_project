using System.Net.Http.Json;
using Thorns.Blazor.Models;

namespace Thorns.Blazor.Services;

public class UserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // =====================================
    // LISTAR USUÁRIOS (ADMIN)
    // =====================================

    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync("api/Users");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<UserResponse>>()
            ?? new List<UserResponse>();
    }

    public async Task<UserProfileResponse?> GetOwnProfileAsync()
    {
        var response = await _httpClient.GetAsync("api/Users/me");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<UserProfileResponse>();
    }

    public async Task UpdateOwnProfileAsync(UserProfileForm request)
    {
        var response = await _httpClient.PutAsJsonAsync(
            "api/Users/me",
            request
        );

        response.EnsureSuccessStatusCode();
    }

    public async Task<UserResponse?> GetUserByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/Users/{id}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserResponse>();
    }

    public async Task UpdateUserAsync(int id, AdminUserForm request)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Users/{id}",
            request
        );

        response.EnsureSuccessStatusCode();
    }
}