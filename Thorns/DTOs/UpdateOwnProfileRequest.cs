using System.ComponentModel.DataAnnotations;
using Thorns.Validation;

namespace Thorns.DTOs;

/// <summary>Dados básicos que o usuário pode alterar no próprio perfil.</summary>
public sealed record UpdateOwnProfileRequest
{
    /// <summary>Nome do usuário.</summary>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public required string Name { get; init; }

    /// <summary>E-mail usado para acessar a conta.</summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public required string Email { get; init; }

    /// <summary>CPF com ou sem pontuação.</summary>
    [Required(ErrorMessage = "O CPF é obrigatório.")]
    [Cpf(ErrorMessage = "Informe um CPF válido.")]
    public required string Cpf { get; init; }

    /// <summary>CEP no formato 00000000 ou 00000-000.</summary>
    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [Cep(ErrorMessage = "Informe um CEP válido.")]
    public required string Cep { get; init; }
}
