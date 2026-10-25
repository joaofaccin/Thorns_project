using System.Net.Http.Headers;

namespace Thorns.Blazor.Services;

public class ApiAuthorizationHandler : DelegatingHandler
{
    private readonly CustomAuthStateProvider _authStateProvider;

    public ApiAuthorizationHandler(
        CustomAuthStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Restaura a sessão, caso necessário.
        await _authStateProvider.GetAuthenticationStateAsync();

        var token = _authStateProvider.Token;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}