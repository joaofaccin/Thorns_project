using System.ComponentModel.DataAnnotations;

namespace Thorns.DTOs;

/// <summary>Dados para criar uma conta comum sem privilégios administrativos.</summary>
public sealed record RegisterUserRequest
{
    /// <summary>Nome do usuário.</summary>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3)]
    public required string Name { get; init; }

    /// <summary>E-mail usado para acessar a conta.</summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    public required string Email { get; init; }

    /// <summary>Senha com no mínimo 6 caracteres.</summary>
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; init; }
}
