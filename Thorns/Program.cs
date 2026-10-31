using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Thorns.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONTROLLERS / JSON
// ==========================================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

// ==========================================
// DATABASE / ENTITY FRAMEWORK
// ==========================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    );
});

// ==========================================
// JWT CONFIG
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key não configurada."
    );

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer não configurado."
    );

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience não configurado."
    );

// ==========================================
// AUTHENTICATION / JWT
// ==========================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Validação do token JWT
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

        // ==========================================
        // EVENTOS JWT
        // ==========================================

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);
                var roleClaim = context.Principal?
                    .FindFirstValue(ClaimTypes.Role);
                var tokenVersionClaim = context.Principal?
                    .FindFirstValue("token_version");

                if (!int.TryParse(userIdClaim, out var userId) ||
                    !int.TryParse(tokenVersionClaim, out var tokenVersion))
                {
                    context.Fail("Token sem informações de sessão válidas.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();
                var currentUser = await dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new
                    {
                        user.IsActive,
                        user.DeletedAt,
                        user.Role,
                        user.TokenVersion
                    })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (currentUser is null ||
                    !currentUser.IsActive ||
                    currentUser.DeletedAt is not null ||
                    tokenVersion != currentUser.TokenVersion ||
                    !string.Equals(
                        roleClaim,
                        currentUser.Role.ToString(),
                        StringComparison.Ordinal))
                {
                    context.Fail("Token revogado ou usuário sem acesso.");
                }
            },

            // Falha ao validar o token
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine();
                Console.WriteLine("=========================");
                Console.WriteLine("ERRO DE AUTENTICAÇÃO JWT");
                Console.WriteLine("=========================");
                Console.WriteLine(context.Exception.Message);
                Console.WriteLine();

                return Task.CompletedTask;
            },

            // Usuário autenticado, mas sem permissão
            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                context.Response.ContentType =
                    "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    status = 403,
                    message =
                        "Você não tem permissão para realizar esta operação."
                });
            },

            // Token ausente, inválido ou expirado
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.ContentType =
                    "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    status = 401,
                    message =
                        "Token ausente, inválido ou expirado."
                });
            }
        };
    });

// ==========================================
// AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// LOGIN RATE LIMITING
// ==========================================

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("registration", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/problem+json";
        var isRegistration = context.HttpContext.Request.Path.Value?
            .EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = isRegistration
                    ? "Muitas tentativas de cadastro"
                    : "Muitas tentativas de login",
                Detail = "Aguarde um minuto antes de tentar novamente."
            },
            cancellationToken);
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    foreach (var proxyAddress in builder.Configuration
                 .GetSection("ForwardedHeaders:KnownProxies")
                 .Get<string[]>() ?? [])
    {
        if (!IPAddress.TryParse(proxyAddress, out var address))
        {
            throw new InvalidOperationException(
                $"Endereço de proxy inválido em ForwardedHeaders:KnownProxies: {proxyAddress}");
        }

        options.KnownProxies.Add(address);
    }
});

// ==========================================
// CORS / BLAZOR
// ==========================================

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (builder.Environment.IsDevelopment())
{
    allowedOrigins = allowedOrigins
        .Concat([
            "http://localhost:5020",
            "https://localhost:7140"
        ])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// ==========================================
// SWAGGER
// ==========================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Documentação da API
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Thorns API",
            Version = "v1"
        }
    );

    // ==========================================
    // JWT NO SWAGGER
    // ==========================================

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description = "Digite o token JWT."
        }
    );

    // Adiciona autenticação Bearer ao Swagger
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document
                )
            ] = []
        }
    );
});

// ==========================================
// APP
// ==========================================

var app = builder.Build();

app.UseForwardedHeaders();

// ==========================================
// SWAGGER
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ==========================================
// MIDDLEWARES
// ==========================================

// Durante os testes locais em HTTP,
// não redirecionar para HTTPS.
// Em produção, configure HTTPS corretamente.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Define o roteamento
app.UseRouting();

// Aplica a política CORS
app.UseCors("BlazorPolicy");

// Limita tentativas no endpoint de login
app.UseRateLimiter();

// Valida a identidade do usuário pelo JWT
app.UseAuthentication();

// Verifica as permissões do usuário
app.UseAuthorization();

// ==========================================
// CONTROLLERS
// ==========================================

app.MapControllers();

// ==========================================
// RUN
// ==========================================

app.Run();