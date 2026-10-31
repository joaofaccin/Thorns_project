using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Thorns.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CepAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var input = value.ToString()!;
        return Regex.IsMatch(input, @"^(?:[0-9]{8}|[0-9]{5}-[0-9]{3})$")
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "Informe um CEP válido.");
    }
}
