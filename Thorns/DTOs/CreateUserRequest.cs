using System.ComponentModel.DataAnnotations;
using Thorns.Models.Enum;
using Thorns.Validation;

namespace Thorns.DTOs;

public class CreateUserRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage = "O nome deve ter entre 3 e 100 caracteres."
    )]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(
        150,
        ErrorMessage = "O e-mail deve ter no máximo 150 caracteres."
    )]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    [Cpf(ErrorMessage = "Informe um CPF válido.")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [Cep(ErrorMessage = "Informe um CEP válido.")]
    public string Cep { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "A senha deve ter no mínimo 6 caracteres."
    )]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "A função do usuário é obrigatória.")]
    [EnumDataType(typeof(UserRole), ErrorMessage = "Informe uma função válida.")]
    public UserRole Role { get; set; }
}