using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using Thorns.Data;
using Thorns.DTOs;
using Thorns.Models;
using Thorns.Models.Enum;

namespace Thorns.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Where(user => user.DeletedAt == null)
            .Select(user => new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Cpf,
                user.Cep,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> GetOwnProfile(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                user.DeletedAt == null &&
                user.IsActive,
                cancellationToken);

        return user is null
            ? NotFound()
            : Ok(ToProfileResponse(user));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> UpdateOwnProfile(
        UpdateOwnProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                user.DeletedAt == null &&
                user.IsActive,
                cancellationToken);

        if (user is null)
            return NotFound();

        var email = NormalizeEmail(request.Email);
        var cpf = NormalizeDigits(request.Cpf);
        var duplicate = await _context.Users.AnyAsync(other =>
            other.Id != userId &&
            (other.Email == email || other.Cpf == cpf),
            cancellationToken);

        if (duplicate)
            return Conflict("Já existe um usuário com esse e-mail ou CPF.");

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Cpf = cpf;
        user.Cep = NormalizeDigits(request.Cep);
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Conflict("Já existe um usuário com esse e-mail ou CPF.");
        }

        return Ok(ToProfileResponse(user));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Where(user =>
                user.Id == id &&
                user.DeletedAt == null
            )
            .Select(user => new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Cpf,
                user.Cep,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastLogin
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return NotFound("Usuário não encontrado.");

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Post(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var cpf = NormalizeDigits(request.Cpf);

        var emailExists = await _context.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
            return Conflict("Já existe um usuário com esse e-mail.");

        var cpfExists = await _context.Users
            .AnyAsync(user => user.Cpf == cpf, cancellationToken);

        if (cpfExists)
            return Conflict("Já existe um usuário com esse CPF.");

        var passwordHash =
            BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Cpf = cpf,
            Cep = NormalizeDigits(request.Cep),
            PasswordHash = passwordHash,
            Role = request.Role,
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
            nameof(GetById),
            new { id = user.Id },
            new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Cpf,
                user.Cep,
                user.Role,
                user.IsActive,
                user.CreatedAt
            }
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Put(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == id &&
                user.DeletedAt == null,
                cancellationToken
            );

        if (user == null)
            return NotFound("Usuário não encontrado.");

        var email = NormalizeEmail(request.Email);
        var cpf = NormalizeDigits(request.Cpf);

        var duplicateExists = await _context.Users
            .AnyAsync(otherUser =>
                otherUser.Id != id &&
                (otherUser.Email == email || otherUser.Cpf == cpf),
                cancellationToken
            );

        if (duplicateExists)
            return Conflict("Já existe outro usuário com esse e-mail ou CPF.");

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Cpf = cpf;
        user.Cep = NormalizeDigits(request.Cep);
        if (user.Role != request.Role ||
            !string.IsNullOrWhiteSpace(request.Password))
        {
            user.TokenVersion++;
        }
        user.Role = request.Role;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Conflict("Já existe outro usuário com esse e-mail ou CPF.");
        }

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Cpf,
            user.Cep,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == id &&
                user.DeletedAt == null,
                cancellationToken
            );

        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.TokenVersion++;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
    [HttpPatch("{id:int}/restore")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound(new
            {
                status = 404,
                message = "Usuário não encontrado."
            });
        }

        if (user.DeletedAt == null && user.IsActive)
        {
            return BadRequest(new
            {
                status = 400,
                message = "O usuário já está ativo."
            });
        }

        user.IsActive = true;
        user.DeletedAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        user.TokenVersion++;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            status = 200,
            message = "Usuário restaurado com sucesso.",
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Cpf,
                user.Cep,
                user.Role,
                user.IsActive,
                user.UpdatedAt
            }
        });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId
        );
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string NormalizeDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private static UserProfileResponse ToProfileResponse(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            user.Cpf ?? string.Empty,
            user.Cep ?? string.Empty,
            user.Role.ToString(),
            user.IsActive
        );

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}