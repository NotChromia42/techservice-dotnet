namespace TechService.Api.Models;

/// <summary>
/// DTO para criação e atualização de equipamentos de acordo com a estrutura local.
/// </summary>
public record EquipamentoDTO(
    string Tipo,
    string Marca,
    string Modelo,
    string NumeroSerie,
    string? Observacoes
);