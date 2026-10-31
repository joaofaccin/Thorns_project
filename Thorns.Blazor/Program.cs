using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Thorns.Blazor;
using Thorns.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// =====================================
// COMPONENTES
// =====================================

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// =====================================
// AUTENTICAÇÃO
// =====================================

builder.Services.AddAuthorizationCore();

// Provider que mantém o usuário autenticado e seu JWT
builder.Services.AddScoped<CustomAuthStateProvider>();

builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>()
);

// =====================================
// HTTP CLIENT
// =====================================

var configuredApiBaseAddress = builder.Configuration["ApiBaseAddress"];
var uiBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
Uri apiBaseAddress;

if (string.IsNullOrWhiteSpace(configuredApiBaseAddress) ||
    string.Equals(configuredApiBaseAddress, "same-origin", StringComparison.OrdinalIgnoreCase))
{
    apiBaseAddress = builder.HostEnvironment.IsDevelopment() &&
                     uiBaseAddress.Host is "localhost" or "127.0.0.1"
        ? new Uri(uiBaseAddress.Scheme == Uri.UriSchemeHttps
            ? "https://localhost:7092/"
            : "http://localhost:5234/")
        : uiBaseAddress;
}
else if (Uri.TryCreate(
             configuredApiBaseAddress,
             UriKind.Absolute,
             out var configuredUri) &&
         (configuredUri.Scheme == Uri.UriSchemeHttp ||
          configuredUri.Scheme == Uri.UriSchemeHttps))
{
    apiBaseAddress = configuredUri;
}
else
{
    throw new InvalidOperationException(
        "ApiBaseAddress deve ser 'same-origin' ou uma URL HTTP/HTTPS absoluta.");
}

// HttpClient padrão utilizado pelo AuthService.
// O login não precisa enviar JWT.
builder.Services.AddScoped<HttpClient>(sp =>
    new HttpClient
    {
        BaseAddress = apiBaseAddress
    }
);

// Handler que adiciona o JWT nas requisições
builder.Services.AddTransient<ApiAuthorizationHandler>();

// UserService com HttpClient autenticado
builder.Services.AddScoped<UserService>(sp =>
{
    var handler = sp.GetRequiredService<ApiAuthorizationHandler>();

    handler.InnerHandler = new HttpClientHandler();

    var client = new HttpClient(handler)
    {
        BaseAddress = apiBaseAddress
    };

    return new UserService(client);
});

// =====================================
// SERVIÇOS
// =====================================

builder.Services.AddScoped<AuthService>();

// =====================================
// INICIAR
// =====================================

await builder.Build().RunAsync();