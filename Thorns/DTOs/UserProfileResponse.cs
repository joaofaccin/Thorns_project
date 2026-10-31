namespace Thorns.DTOs;

/// <summary>Dados básicos de perfil retornados para consulta autorizada.</summary>
public sealed record UserProfileResponse(
    int Id,
    string Name,
    string Email,
    string Cpf,
    string Cep,
    string Role,
    bool IsActive);
