using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Swagger/OpenAPI - Swagger Desabilitado já que não é necessário para situações de produção, mas pode ser habilitado para desenvolvimento local.
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
// }

// Factory para gerir conexões MySQL reutilizáveis
builder.Services.AddSingleton<MySqlConnectionFactory>();

var app = builder.Build();


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
        "GET /api/clientes", "GET /api/clientes/{id}", "POST /api/clientes", "PUT /api/clientes/{id}", "DELETE /api/clientes/{id}",
        "GET /api/equipamentos", "GET /api/equipamentos/{id}", "POST /api/equipamentos", "PUT /api/equipamentos/{id}", "DELETE /api/equipamentos/{id}",
        "GET /api/ordens-servico", "GET /api/ordens-servico/{id}", "POST /api/ordens-servico", "PUT /api/ordens-servico/{id}", "DELETE /api/ordens-servico/{id}"
    }
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.Produces(StatusCodes.Status200OK);

// =============================================================================
// 1. CLIENTES ENDPOINTS
// =============================================================================

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
.Produces<List<Cliente>>(StatusCodes.Status200OK);

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
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não foi encontrado." });
    }

    return Results.Ok(MapReaderToCliente(reader));
})
.WithName("ObterClientePorId")
.WithSummary("Obter um cliente específico por ID")
.Produces<Cliente>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

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
.Produces(StatusCodes.Status400BadRequest);

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
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não foi encontrado." });
    }

    return Results.Ok(new { mensagem = "Cliente atualizado com sucesso!" });
})
.WithName("AtualizarCliente")
.WithSummary("Atualizar dados de um cliente")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

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
        return Results.NotFound(new { mensagem = $"Cliente com ID {id} não foi encontrado." });
    }

    return Results.Ok(new { mensagem = "Cliente desativado com sucesso!" });
})
.WithName("RemoverCliente")
.WithSummary("Remover um cliente")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// =============================================================================
// 2. EQUIPAMENTOS ENDPOINTS
// =============================================================================

app.MapGet("/api/equipamentos", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_equipamento, tipo, marca, modelo, numero_serie, observacoes, status, created_at, updated_at, deleted_at
        FROM equipamentos
        WHERE status != 0
        ORDER BY id_equipamento DESC;
        """;

    var equipamentos = new List<Equipamento>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        equipamentos.Add(MapReaderToEquipamento(reader));
    }

    return Results.Ok(equipamentos);
})
.WithName("ListarEquipamentos")
.WithSummary("Listar todos os equipamentos ativos")
.Produces<List<Equipamento>>(StatusCodes.Status200OK);

app.MapGet("/api/equipamentos/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_equipamento, tipo, marca, modelo, numero_serie, observacoes, status, created_at, updated_at, deleted_at
        FROM equipamentos
        WHERE id_equipamento = @id AND status != 0;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { mensagem = $"Equipamento com ID {id} não foi encontrado." });
    }

    return Results.Ok(MapReaderToEquipamento(reader));
})
.WithName("ObterEquipamentoPorId")
.WithSummary("Obter um equipamento por ID")
.Produces<Equipamento>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/equipamentos", async (EquipamentoDTO dto, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(dto.Tipo) || string.IsNullOrWhiteSpace(dto.Marca) || string.IsNullOrWhiteSpace(dto.Modelo) || string.IsNullOrWhiteSpace(dto.NumeroSerie))
    {
        return Results.BadRequest(new { mensagem = "Os campos 'Tipo', 'Marca', 'Modelo' e 'NumeroSerie' são obrigatórios." });
    }

    const string sql = """
        INSERT INTO equipamentos (tipo, marca, modelo, numero_serie, observacoes, status, created_at)
        VALUES (@tipo, @marca, @modelo, @numero_serie, @observacoes, 1, NOW());
        SELECT LAST_INSERT_ID();
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@tipo", dto.Tipo);
    command.Parameters.AddWithValue("@marca", dto.Marca);
    command.Parameters.AddWithValue("@modelo", dto.Modelo);
    command.Parameters.AddWithValue("@numero_serie", dto.NumeroSerie);
    command.Parameters.AddWithValue("@observacoes", (object?)dto.Observacoes ?? DBNull.Value);

    try
    {
        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return Results.Created($"/api/equipamentos/{newId}", new { id_equipamento = newId, mensagem = "Equipamento criado com sucesso!" });
    }
    catch (MySqlException ex) when (ex.Number == 1062)
    {
        return Results.BadRequest(new { mensagem = "Já existe um equipamento com este Número de Série." });
    }
})
.WithName("CriarEquipamento")
.WithSummary("Criar um novo equipamento")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapPut("/api/equipamentos/{id:int}", async (int id, EquipamentoDTO dto, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(dto.Tipo) || string.IsNullOrWhiteSpace(dto.Marca) || string.IsNullOrWhiteSpace(dto.Modelo) || string.IsNullOrWhiteSpace(dto.NumeroSerie))
    {
        return Results.BadRequest(new { mensagem = "Os campos 'Tipo', 'Marca', 'Modelo' e 'NumeroSerie' são obrigatórios." });
    }

    const string sql = """
        UPDATE equipamentos
        SET tipo = @tipo,
            marca = @marca,
            modelo = @modelo,
            numero_serie = @numero_serie,
            observacoes = @observacoes,
            updated_at = NOW()
        WHERE id_equipamento = @id AND status != 0;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@tipo", dto.Tipo);
    command.Parameters.AddWithValue("@marca", dto.Marca);
    command.Parameters.AddWithValue("@modelo", dto.Modelo);
    command.Parameters.AddWithValue("@numero_serie", dto.NumeroSerie);
    command.Parameters.AddWithValue("@observacoes", (object?)dto.Observacoes ?? DBNull.Value);

    try
    {
        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return Results.NotFound(new { mensagem = $"Equipamento com ID {id} não foi encontrado." });
        }

        return Results.Ok(new { mensagem = "Equipamento atualizado com sucesso!" });
    }
    catch (MySqlException ex) when (ex.Number == 1062)
    {
        return Results.BadRequest(new { mensagem = "Número de Série já pertence a outro equipamento." });
    }
})
.WithName("AtualizarEquipamento")
.WithSummary("Atualizar dados de um equipamento")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/api/equipamentos/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE equipamentos
        SET status = 0, deleted_at = NOW()
        WHERE id_equipamento = @id AND status != 0;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);

    int rowsAffected = await command.ExecuteNonQueryAsync();

    if (rowsAffected == 0)
    {
        return Results.NotFound(new { mensagem = $"Equipamento com ID {id} não foi encontrado." });
    }

    return Results.Ok(new { mensagem = "Equipamento desativado com sucesso!" });
})
.WithName("RemoverEquipamento")
.WithSummary("Remover um equipamento")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// =============================================================================
// 3. ORDENS DE SERVIÇO ENDPOINTS
// =============================================================================

// READ ALL
app.MapGet("/api/ordens-servico", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_ordem, id_equipamento, defeito_relatado, diagnostico, solucao, status, prioridade,
               valor_servico, valor_pecas, desconto, valor_total, created_at, updated_at, deleted_at
        FROM ordens_servico
        WHERE deleted_at IS NULL
        ORDER BY id_ordem DESC;
        """;

    var ordens = new List<OrdemServico>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        ordens.Add(MapReaderToOrdemServico(reader));
    }

    return Results.Ok(ordens);
})
.WithName("ListarOrdensServico")
.WithSummary("Listar todas as ordens de serviço ativas")
.Produces<List<OrdemServico>>(StatusCodes.Status200OK);

// READ BY ID
app.MapGet("/api/ordens-servico/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_ordem, id_equipamento, defeito_relatado, diagnostico, solucao, status, prioridade,
               valor_servico, valor_pecas, desconto, valor_total, created_at, updated_at, deleted_at
        FROM ordens_servico
        WHERE id_ordem = @id AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { mensagem = $"Ordem de Serviço com ID {id} não foi encontrada." });
    }

    return Results.Ok(MapReaderToOrdemServico(reader));
})
.WithName("ObterOrdemServicoPorId")
.WithSummary("Obter uma Ordem de Serviço por ID")
.Produces<OrdemServico>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// CREATE
app.MapPost("/api/ordens-servico", async (OrdemServicoDTO dto, MySqlConnectionFactory factory) =>
{
    if (dto.IdEquipamento <= 0 || string.IsNullOrWhiteSpace(dto.DefeitoRelatado))
    {
        return Results.BadRequest(new { mensagem = "Os campos 'IdEquipamento' e 'DefeitoRelatado' são obrigatórios." });
    }

    const string sql = """
        INSERT INTO ordens_servico 
        (id_equipamento, defeito_relatado, diagnostico, solucao, status, prioridade, valor_servico, valor_pecas, desconto, created_at)
        VALUES 
        (@id_equipamento, @defeito_relatado, @diagnostico, @solucao, @status, @prioridade, @valor_servico, @valor_pecas, @desconto, NOW());
        SELECT LAST_INSERT_ID();
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_equipamento", dto.IdEquipamento);
    command.Parameters.AddWithValue("@defeito_relatado", dto.DefeitoRelatado);
    command.Parameters.AddWithValue("@diagnostico", (object?)dto.Diagnostico ?? DBNull.Value);
    command.Parameters.AddWithValue("@solucao", (object?)dto.Solucao ?? DBNull.Value);
    command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(dto.Status) ? "ABERTA" : dto.Status);
    command.Parameters.AddWithValue("@prioridade", string.IsNullOrWhiteSpace(dto.Prioridade) ? "MEDIA" : dto.Prioridade);
    command.Parameters.AddWithValue("@valor_servico", dto.ValorServico);
    command.Parameters.AddWithValue("@valor_pecas", dto.ValorPecas);
    command.Parameters.AddWithValue("@desconto", dto.Desconto);

    try
    {
        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return Results.Created($"/api/ordens-servico/{newId}", new { id_ordem = newId, mensagem = "Ordem de Serviço criada com sucesso!" });
    }
    catch (MySqlException ex) when (ex.Number == 1452) // FK errada no id_equipamento
    {
        return Results.BadRequest(new { mensagem = "O Equipamento associado (id_equipamento) não existe." });
    }
})
.WithName("CriarOrdemServico")
.WithSummary("Criar uma nova Ordem de Serviço")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

// UPDATE
app.MapPut("/api/ordens-servico/{id:int}", async (int id, OrdemServicoDTO dto, MySqlConnectionFactory factory) =>
{
    if (dto.IdEquipamento <= 0 || string.IsNullOrWhiteSpace(dto.DefeitoRelatado))
    {
        return Results.BadRequest(new { mensagem = "Os campos 'IdEquipamento' e 'DefeitoRelatado' são obrigatórios." });
    }

    const string sql = """
        UPDATE ordens_servico
        SET id_equipamento = @id_equipamento,
            defeito_relatado = @defeito_relatado,
            diagnostico = @diagnostico,
            solucao = @solucao,
            status = @status,
            prioridade = @prioridade,
            valor_servico = @valor_servico,
            valor_pecas = @valor_pecas,
            desconto = @desconto,
            updated_at = NOW()
        WHERE id_ordem = @id AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@id_equipamento", dto.IdEquipamento);
    command.Parameters.AddWithValue("@defeito_relatado", dto.DefeitoRelatado);
    command.Parameters.AddWithValue("@diagnostico", (object?)dto.Diagnostico ?? DBNull.Value);
    command.Parameters.AddWithValue("@solucao", (object?)dto.Solucao ?? DBNull.Value);
    command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(dto.Status) ? "ABERTA" : dto.Status);
    command.Parameters.AddWithValue("@prioridade", string.IsNullOrWhiteSpace(dto.Prioridade) ? "MEDIA" : dto.Prioridade);
    command.Parameters.AddWithValue("@valor_servico", dto.ValorServico);
    command.Parameters.AddWithValue("@valor_pecas", dto.ValorPecas);
    command.Parameters.AddWithValue("@desconto", dto.Desconto);

    try
    {
        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return Results.NotFound(new { mensagem = $"Ordem de Serviço com ID {id} não foi encontrada." });
        }

        return Results.Ok(new { mensagem = "Ordem de Serviço atualizada com sucesso!" });
    }
    catch (MySqlException ex) when (ex.Number == 1452)
    {
        return Results.BadRequest(new { mensagem = "O Equipamento associado (id_equipamento) não existe." });
    }
})
.WithName("AtualizarOrdemServico")
.WithSummary("Atualizar uma Ordem de Serviço existente")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

// DELETE (Soft Delete via deleted_at)
app.MapDelete("/api/ordens-servico/{id:int}", async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE ordens_servico
        SET deleted_at = NOW()
        WHERE id_ordem = @id AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);

    int rowsAffected = await command.ExecuteNonQueryAsync();

    if (rowsAffected == 0)
    {
        return Results.NotFound(new { mensagem = $"Ordem de Serviço com ID {id} não foi encontrada ou já foi removida." });
    }

    return Results.Ok(new { mensagem = "Ordem de Serviço removida com sucesso!" });
})
.WithName("RemoverOrdemServico")
.WithSummary("Remover uma Ordem de Serviço")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Inicializa a aplicação .NET
app.Run();

// -----------------------------------------------------------------------------
// HELPER METHODS
// -----------------------------------------------------------------------------

static Cliente MapReaderToCliente(MySqlDataReader reader)
{
    return new Cliente
    {
        IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
        Nome = reader.GetString(reader.GetOrdinal("nome")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        Telefone = reader.IsDBNull(reader.GetOrdinal("telefone")) ? null : reader.GetString(reader.GetOrdinal("telefone")),
        Status = reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("deleted_at"))
    };
}

static Equipamento MapReaderToEquipamento(MySqlDataReader reader)
{
    return new Equipamento
    {
        IdEquipamento = reader.GetInt32(reader.GetOrdinal("id_equipamento")),
        Tipo = reader.GetString(reader.GetOrdinal("tipo")),
        Marca = reader.GetString(reader.GetOrdinal("marca")),
        Modelo = reader.GetString(reader.GetOrdinal("modelo")),
        NumeroSerie = reader.GetString(reader.GetOrdinal("numero_serie")),
        Observacoes = reader.IsDBNull(reader.GetOrdinal("observacoes")) ? null : reader.GetString(reader.GetOrdinal("observacoes")),
        Status = reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("deleted_at"))
    };
}

static OrdemServico MapReaderToOrdemServico(MySqlDataReader reader)
{
    return new OrdemServico
    {
        IdOrdem = reader.GetInt32(reader.GetOrdinal("id_ordem")),
        IdEquipamento = reader.GetInt32(reader.GetOrdinal("id_equipamento")),
        DefeitoRelatado = reader.GetString(reader.GetOrdinal("defeito_relatado")),
        Diagnostico = reader.IsDBNull(reader.GetOrdinal("diagnostico")) ? null : reader.GetString(reader.GetOrdinal("diagnostico")),
        Solucao = reader.IsDBNull(reader.GetOrdinal("solucao")) ? null : reader.GetString(reader.GetOrdinal("solucao")),
        Status = reader.GetString(reader.GetOrdinal("status")),
        Prioridade = reader.GetString(reader.GetOrdinal("prioridade")),
        ValorServico = reader.GetDecimal(reader.GetOrdinal("valor_servico")),
        ValorPecas = reader.GetDecimal(reader.GetOrdinal("valor_pecas")),
        Desconto = reader.GetDecimal(reader.GetOrdinal("desconto")),
        ValorTotal = reader.GetDecimal(reader.GetOrdinal("valor_total")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("deleted_at"))
    };
}