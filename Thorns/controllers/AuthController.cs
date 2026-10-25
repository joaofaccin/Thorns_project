using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Thorns.Data;
using Thorns.DTOs;

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
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.DeletedAt == null
            );

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
            return Unauthorized("Usuário inativo.");
        }

        // =========================
        // ATUALIZA ÚLTIMO LOGIN
        // =========================

        user.LastLogin = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
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
                u.DeletedAt == null
            );

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
}