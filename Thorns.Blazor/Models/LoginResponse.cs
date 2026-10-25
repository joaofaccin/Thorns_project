namespace Thorns.Blazor.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}