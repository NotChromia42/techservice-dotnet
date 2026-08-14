using System.Text.Json.Serialization;

namespace TechService.Api.Models;

/// <summary>
/// DTO para criação e atualização de equipamentos de acordo com a estrutura local.
/// </summary>
public record EquipamentoDTO(
    [property: JsonPropertyName("id_cliente")] int IdCliente,
    string Tipo,
    string Marca,
    string Modelo,
    string NumeroSerie,
    string? Observacoes
);