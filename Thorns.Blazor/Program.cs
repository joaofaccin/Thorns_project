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

// HttpClient padrão utilizado pelo AuthService.
// O login não precisa enviar JWT.
builder.Services.AddScoped<HttpClient>(sp =>
    new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5234/")
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
        BaseAddress = new Uri("http://localhost:5234/")
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