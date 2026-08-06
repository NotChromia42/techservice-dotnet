var builder = WebApplication.CreateBuilder(args);

// Serviços necessários para gerar a documentação OpenAPI/Swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// O Swagger fica disponível apenas no ambiente de desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint inicial da Versão 0.
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "Olá! Bem-vindo à API TechService - Versão 0",
    versao = "V0",
    estado = "API mínima em funcionamento"
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.WithDescription("Endpoint de teste da Versão 0.")
.Produces(StatusCodes.Status200OK);

app.Run();
