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

    // =====================================
    // USUÁRIO AUTENTICADO
    // =====================================

    public async Task<CurrentUserResponse?> GetMeAsync()
    {
        var response = await _httpClient.GetAsync("api/Auth/me");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<CurrentUserResponse>();
    }
}