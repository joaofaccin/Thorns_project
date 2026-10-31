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

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
            !string.IsNullOrWhiteSpace(token))
        {
            await _authStateProvider.LogoutAsync();
        }

        return response;
    }
}