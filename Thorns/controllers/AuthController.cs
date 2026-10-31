using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Thorns.Data;
using Thorns.DTOs;
using Thorns.Models;
using Thorns.Models.Enum;

namespace Thorns.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.DeletedAt == null,
                cancellationToken);

        if (user == null)
        {
            return Unauthorized("E-mail ou senha inválidos.");
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash
        );

        if (!passwordValid)
        {
            return Unauthorized("E-mail ou senha inválidos.");
        }

        if (!user.IsActive)
        {
            return Unauthorized("E-mail ou senha inválidos.");
        }

        // =========================
        // ATUALIZA ÚLTIMO LOGIN
        // =========================

        user.LastLogin = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // =========================
        // CONFIGURAÇÕES JWT
        // =========================

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key não configurada."
            );

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer não configurado."
            );

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience não configurado."
            );

        // =========================
        // CLAIMS
        // =========================

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                user.Name
            ),

            new Claim(
                ClaimTypes.Email,
                user.Email
            ),

            new Claim(
                ClaimTypes.Role,
                user.Role.ToString()
            ),

            new Claim(
                "token_version",
                user.TokenVersion.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
            )
        };

        // =========================
        // CHAVE JWT
        // =========================

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // =========================
        // TEMPO DE EXPIRAÇÃO
        // =========================

        var expirationMinutes = _configuration.GetValue<int>(
            "Jwt:ExpirationMinutes",
            60
        );

        if (expirationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Jwt:ExpirationMinutes deve ser maior que zero."
            );
        }

        // =========================
        // CRIA TOKEN
        // =========================

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        // =========================
        // RESPONSE
        // =========================

        return Ok(new
        {
            token = tokenString,

            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role
            }
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var duplicate = await _context.Users.AnyAsync(user => user.Email == email,
            cancellationToken);

        if (duplicate)
            return Conflict("Já existe um usuário com esse e-mail.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User,
            IsActive = true
        };

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Conflict("Já existe um usuário com esse e-mail ou CPF.");
        }

        return CreatedAtAction(
            nameof(UsersController.GetById),
            "Users",
            new { id = user.Id },
            new
            {
                user.Id,
                user.Name,
                user.Email,
                Role = user.Role.ToString()
            });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        // Obtém o ID do usuário pelo JWT
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        // Converte o ID para int
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Token inválido."
            });
        }

        // Busca o usuário no PostgreSQL
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.DeletedAt == null,
                cancellationToken);

        if (user == null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Usuário não encontrado ou inativo."
            });
        }

        // Retorna os dados do usuário autenticado
        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.LastLogin
        });
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}