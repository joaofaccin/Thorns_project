using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Thorns.Blazor.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CpfAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        var input = value.ToString()!;
        if (!Regex.IsMatch(input, @"^(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})$"))
            return new ValidationResult(ErrorMessage ?? "Informe um CPF válido.");

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || digits.All(digit => digit == digits[0]))
            return new ValidationResult(ErrorMessage ?? "Informe um CPF válido.");

        var firstDigit = CalculateCheckDigit(digits, 9);
        var secondDigit = CalculateCheckDigit(digits, 10);

        return digits[9] - '0' == firstDigit &&
               digits[10] - '0' == secondDigit
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "Informe um CPF válido.");
    }

    private static int CalculateCheckDigit(string cpf, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
            sum += (cpf[index] - '0') * (length + 1 - index);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
