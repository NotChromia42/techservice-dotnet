namespace TechService.Api.Models;

/// <summary>
/// DTO utilizado para a criação e atualização de clientes.
/// Impede que o cliente envie campos gerados automaticamente pelo MySQL (como id_cliente ou created_at).
/// </summary>
public record ClienteDTO(string Nome, string Email, string? Telefone);