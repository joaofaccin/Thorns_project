using System.ComponentModel.DataAnnotations;

namespace Thorns.Blazor.Models;

public sealed class AdminUserForm : UserProfileForm
{
    [Required(ErrorMessage = "A função do usuário é obrigatória.")]
    [RegularExpression("^(User|Admin)$", ErrorMessage = "Selecione uma função válida.")]
    public string Role { get; set; } = "User";

    [RegularExpression(@"^$|^.{6,100}$", ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
    public string? Password { get; set; }
}
