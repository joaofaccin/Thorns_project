using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;
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
// CORS / BLAZOR
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5020")
            .AllowAnyHeader()
            .AllowAnyMethod();
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