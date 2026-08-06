using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Serviços usados pelo Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Uma única factory reutilizada para criar ligações ao MySQL.
builder.Services.AddSingleton<MySqlConnectionFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint de estado atualizado para a Versão 2.
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "Olá! Bem-vindo à API TechService - Versão 2",
    versao = "V2",
    estado = "API ligada ao MySQL",
    endpoints_disponiveis = new[]
    {
        "GET /api/clientes",
        "GET /api/clientes/{id}"
    }
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.Produces(StatusCodes.Status200OK);

// Versão 1: Listar todos os clientes ativos da tabela clientes.
app.MapGet("/api/clientes", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_cliente,
            nome,
            email,
            telefone,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM clientes
        WHERE status = 1
        ORDER BY nome;
        """;

    var clientes = new List<Cliente>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var ordinalIdCliente = reader.GetOrdinal("id_cliente");
    var ordinalNome = reader.GetOrdinal("nome");
    var ordinalEmail = reader.GetOrdinal("email");
    var ordinalTelefone = reader.GetOrdinal("telefone");
    var ordinalStatus = reader.GetOrdinal("status");
    var ordinalCreatedAt = reader.GetOrdinal("created_at");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    while (await reader.ReadAsync())
    {
        clientes.Add(new Cliente
        {
            IdCliente = reader.GetInt32(ordinalIdCliente),
            Nome = reader.GetString(ordinalNome),
            Email = reader.GetString(ordinalEmail),
            Telefone = reader.IsDBNull(ordinalTelefone)
                ? null
                : reader.GetString(ordinalTelefone),
            Status = reader.GetInt32(ordinalStatus),
            CreatedAt = reader.GetDateTime(ordinalCreatedAt),
            UpdatedAt = reader.IsDBNull(ordinalUpdatedAt)
                ? null
                : reader.GetDateTime(ordinalUpdatedAt),
            DeletedAt = reader.IsDBNull(ordinalDeletedAt)
                ? null
                : reader.GetDateTime(ordinalDeletedAt)
        });
    }

    return Results.Ok(clientes);
})
.WithName("ListarClientes")
.WithSummary("Listar clientes ativos")
.WithDescription("Devolve os clientes da tabela clientes cujo status é igual a 1.")
.Produces<List<Cliente>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

// Versão 2: Obter cliente ativo por ID.
app.MapGet("/api/clientes/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_cliente,
            nome,
            email,
            telefone,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM clientes
        WHERE id_cliente = @id AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { mensagem = $"Cliente com o ID {id} não foi encontrado ou está inativo." });
    }

    var cliente = new Cliente
    {
        IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
        Nome = reader.GetString(reader.GetOrdinal("nome")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        Telefone = reader.IsDBNull(reader.GetOrdinal("telefone"))
            ? null
            : reader.GetString(reader.GetOrdinal("telefone")),
        Status = reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("updated_at")),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("deleted_at"))
    };

    return Results.Ok(cliente);
})
.WithName("ObterClientePorId")
.WithSummary("Obter cliente por ID")
.WithDescription("Devolve os dados de um cliente ativo específico a partir do seu ID.")
.Produces<Cliente>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();