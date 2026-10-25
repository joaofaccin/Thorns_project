using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> Get()
    {
        var users = await _context.Users
            .Where(user => user.DeletedAt == null)
            .Select(user => new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GetById(int id)
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
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastLogin
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("Usuário não encontrado.");

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Post(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var emailExists = await _context.Users
            .AnyAsync(user => user.Email == email);

        if (emailExists)
            return BadRequest("Já existe um usuário com esse e-mail.");

        var passwordHash =
            BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            Role = request.Role,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            new
            {
                user.Id,
                user.Name,
                user.Email,
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
        UpdateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == id &&
                user.DeletedAt == null
            );

        if (user == null)
            return NotFound("Usuário não encontrado.");

        var email = request.Email.Trim().ToLower();

        var emailExists = await _context.Users
            .AnyAsync(otherUser =>
                otherUser.Email == email &&
                otherUser.Id != id
            );

        if (emailExists)
            return BadRequest("Já existe outro usuário com esse e-mail.");

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Role = request.Role;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == id &&
                user.DeletedAt == null
            );

        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPatch("{id:int}/restore")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Restore(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user => user.Id == id);

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

        await _context.SaveChangesAsync();

        return Ok(new
        {
            status = 200,
            message = "Usuário restaurado com sucesso.",
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.UpdatedAt
            }
        });
    }
}