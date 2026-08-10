using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Factory para gerir conexões MySQL reutilizáveis
builder.Services.AddSingleton<MySqlConnectionFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -----------------------------------------------------------------------------
// 0. HEALTH / STATUS ENDPOINT
// -----------------------------------------------------------------------------
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "Olá! Bem-vindo à API TechService - Versão 3",
    versao = "V3",
    estado = "API ligada ao MySQL com CRUD completo",
    endpoints_disponiveis = new[]
    {
        "GET /api/clientes",
        "GET /api/clientes/{id}",
        "POST /api/clientes",
        "PUT /api/clientes/{id}",
        "DELETE /api/clientes/{id}"
    }
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.Produces(StatusCodes.Status200OK);

// -----------------------------------------------------------------------------
// 1. READ ALL (Listar clientes ativos)
// -----------------------------------------------------------------------------
app.MapGet("/api/clientes", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_cliente, nome, email, telefone, status, created_at, updated_at, deleted_at
        FROM clientes
        WHERE status = 1
        ORDER BY nome;
        """;

    var clientes = new List<Cliente>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        clientes.Add(MapReaderToCliente(reader));
    }

    return Results.Ok(clientes);
})
.WithName("ListarClientes")
.WithSummary("Listar todos os clientes ativos")
.Produces<List<Cliente>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

// -----------------------------------------------------------------------------
// 2. READ BY ID (Obter cliente por ID)
// -----------------------------------------------------------------------------
app.MapGet("/api/clientes/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_cliente, nome, email, telefone, status, created_at, updated_at, deleted_at
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
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não foi encontrado ou está inativo." });
    }

    return Results.Ok(MapReaderToCliente(reader));
})
.WithName("ObterClientePorId")
.WithSummary("Obter um cliente específico por ID")
.Produces<Cliente>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

// -----------------------------------------------------------------------------
// 3. CREATE (Criar novo cliente)
// -----------------------------------------------------------------------------
app.MapPost("/api/clientes", async (ClienteDTO dto, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email))
    {
        return Results.BadRequest(new { mensagem = "Os campos 'Nome' e 'Email' são obrigatórios." });
    }

    const string sql = """
        INSERT INTO clientes (nome, email, telefone, status, created_at)
        VALUES (@nome, @email, @telefone, 1, NOW());
        SELECT LAST_INSERT_ID();
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@nome", dto.Nome);
    command.Parameters.AddWithValue("@email", dto.Email);
    command.Parameters.AddWithValue("@telefone", (object?)dto.Telefone ?? DBNull.Value);

    var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

    return Results.Created($"/api/clientes/{newId}", new { id_cliente = newId, mensagem = "Cliente criado com sucesso!" });
})
.WithName("CriarCliente")
.WithSummary("Criar um novo cliente")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

// -----------------------------------------------------------------------------
// 4. UPDATE (Atualizar cliente existente)
// -----------------------------------------------------------------------------
app.MapPut("/api/clientes/{id:int}", async (int id, ClienteDTO dto, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email))
    {
        return Results.BadRequest(new { mensagem = "Os campos 'Nome' e 'Email' são obrigatórios." });
    }

    const string sql = """
        UPDATE clientes
        SET nome = @nome, email = @email, telefone = @telefone, updated_at = NOW()
        WHERE id_cliente = @id AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@nome", dto.Nome);
    command.Parameters.AddWithValue("@email", dto.Email);
    command.Parameters.AddWithValue("@telefone", (object?)dto.Telefone ?? DBNull.Value);

    int rowsAffected = await command.ExecuteNonQueryAsync();

    if (rowsAffected == 0)
    {
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não foi encontrado para atualização." });
    }

    return Results.Ok(new { mensagem = "Cliente atualizado com sucesso!" });
})
.WithName("AtualizarCliente")
.WithSummary("Atualizar dados de um cliente existente")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

// -----------------------------------------------------------------------------
// 5. DELETE (Soft Delete - Desativar cliente)
// -----------------------------------------------------------------------------
app.MapDelete("/api/clientes/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE clientes
        SET status = 0, deleted_at = NOW()
        WHERE id_cliente = @id AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);

    int rowsAffected = await command.ExecuteNonQueryAsync();

    if (rowsAffected == 0)
    {
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não foi encontrado ou já se encontra desativado." });
    }

    return Results.Ok(new { mensagem = "Cliente desativado com sucesso!" });
})
.WithName("RemoverCliente")
.WithSummary("Remover (soft delete) um cliente")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

// Inicializa a aplicação .NET
app.Run();

// -----------------------------------------------------------------------------
// HELPER METHODS (Sempre no fim do ficheiro, a seguir ao app.Run())
// -----------------------------------------------------------------------------

static Cliente MapReaderToCliente(MySqlDataReader reader)
{
    return new Cliente
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
}