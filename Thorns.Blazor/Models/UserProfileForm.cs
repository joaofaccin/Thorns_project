using System.ComponentModel.DataAnnotations;
using Thorns.Blazor.Validation;

namespace Thorns.Blazor.Models;

public class UserProfileForm
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    [Cpf(ErrorMessage = "Informe um CPF válido.")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [Cep(ErrorMessage = "Informe um CEP válido.")]
    public string Cep { get; set; } = string.Empty;
}
