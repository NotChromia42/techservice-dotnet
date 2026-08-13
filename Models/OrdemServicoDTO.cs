namespace TechService.Api.Models;

public record OrdemServicoDTO(
    int IdEquipamento,
    string DefeitoRelatado,
    string? Diagnostico,
    string? Solucao,
    string Status,
    string Prioridade,
    decimal ValorServico,
    decimal ValorPecas,
    decimal Desconto
);