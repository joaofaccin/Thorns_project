using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace Thorns.Blazor.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "thorns_auth_token";

    private static readonly ClaimsPrincipal Anonymous =
        new(new ClaimsIdentity());

    private readonly IJSRuntime _js;

    private ClaimsPrincipal _currentUser = Anonymous;

    private bool _initialized;

    private CancellationTokenSource? _expirationCancellation;

    public string? Token { get; private set; }

    public CustomAuthStateProvider(IJSRuntime js)
    {
        _js = js;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            _initialized = true;

            try
            {
                var token = await _js.InvokeAsync<string?>(
                    "sessionStorage.getItem",
                    TokenKey
                );

                if (!string.IsNullOrWhiteSpace(token))
                {
                    if (IsTokenExpired(token))
                    {
                        await _js.InvokeVoidAsync(
                            "sessionStorage.removeItem",
                            TokenKey
                        );
                    }
                    else
                    {
                        SetUserFromToken(token);
                    }
                }
            }
            catch (Exception ex) when (
    ex is JSException or
    ArgumentException or
    FormatException or
    JsonException)
            {
                _currentUser = Anonymous;
                Token = null;

                try
                {
                    await _js.InvokeVoidAsync(
                        "sessionStorage.removeItem",
                        TokenKey
                    );
                }
                catch (JSException)
                {
                    // Armazenamento indisponível.
                }
            }
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task SetAuthenticatedUserAsync(string token)
    {
        if (IsTokenExpired(token))
        {
            throw new InvalidOperationException(
                "O token recebido está expirado."
            );
        }

        SetUserFromToken(token);

        _initialized = true;

        await _js.InvokeVoidAsync(
            "sessionStorage.setItem",
            TokenKey,
            token
        );

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser))
        );
    }

    public async Task LogoutAsync()
    {
        _expirationCancellation?.Cancel();
        _expirationCancellation?.Dispose();
        _expirationCancellation = null;

        Token = null;
        _currentUser = Anonymous;
        _initialized = true;

        try
        {
            await _js.InvokeVoidAsync(
                "sessionStorage.removeItem",
                TokenKey
            );
        }
        finally
        {
            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    new AuthenticationState(_currentUser)
                )
            );
        }
    }

    private void SetUserFromToken(string token)
    {
        var claims = ParseClaimsFromJwt(token);

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "jwt"
        );

        Token = token;

        _currentUser = new ClaimsPrincipal(identity);

        StartTokenExpirationMonitor();
    }

    private static bool IsTokenExpired(string token)
    {
        var claims = ParseClaimsFromJwt(token);

        var expiration = claims.FirstOrDefault(
            claim => claim.Type == "exp"
        );

        if (expiration is null ||
            !long.TryParse(expiration.Value, out var seconds))
        {
            return true;
        }

        try
        {
            var expirationDate =
                DateTimeOffset.FromUnixTimeSeconds(seconds);

            return expirationDate <= DateTimeOffset.UtcNow;
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }
    private void StartTokenExpirationMonitor()
    {
        _expirationCancellation?.Cancel();
        _expirationCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        _expirationCancellation = cancellation;

        _ = MonitorTokenExpirationAsync(cancellation.Token);
    }

    private async Task MonitorTokenExpirationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(30)
            );

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(Token))
                    return;

                if (IsTokenExpired(Token))
                {
                    await LogoutAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Monitoramento interrompido durante logout ou novo login.
        }
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');

        if (parts.Length != 3)
        {
            throw new ArgumentException("JWT inválido.");
        }

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        payload = payload.PadRight(
            payload.Length + (4 - payload.Length % 4) % 4,
            '='
        );

        var jsonBytes = Convert.FromBase64String(payload);

        using var document = JsonDocument.Parse(jsonBytes);

        var claims = new List<Claim>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value.ToString();

            var claimType = property.Name switch
            {
                "role" => ClaimTypes.Role,

                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    => ClaimTypes.Role,

                "name" => ClaimTypes.Name,

                "unique_name" => ClaimTypes.Name,

                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                    => ClaimTypes.Name,

                "sub" => ClaimTypes.NameIdentifier,

                "nameid" => ClaimTypes.NameIdentifier,

                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
                    => ClaimTypes.NameIdentifier,

                _ => property.Name
            };

            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    claims.Add(new Claim(claimType, item.ToString()));
                }
            }
            else
            {
                claims.Add(new Claim(claimType, value));
            }
        }

        return claims;
    }
}