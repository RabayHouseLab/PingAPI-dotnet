using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RabayHouseLab.PingApi.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// EF Core — padrão exigido (InMemory para cenário sem persistência externa; pronto para SqlServer/Npgsql)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("PingApiDb"));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Segue convenção camelCase para serialização JSON (padrão System.Text.Json)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// OpenAPI nativo do ASP.NET Core (.NET 9/10) — substitui Swashbuckle no template oficial
// Referência: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Ping API";
        document.Info.Version = "v1";
        document.Info.Description = "Web API minimalista com endpoint de health-check.";
        return Task.CompletedTask;
    });
});

// CORS — necessário para consumo via browser (Blazor WebAssembly / fetch) e desenvolvimento local
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5143", "https://localhost:7223", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Health checks (boa prática para probes Kubernetes / load balancer)
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

// Torna Program acessível para WebApplicationFactory nos testes de integração
// Composition root — excluído de cobertura (boa prática: sem regra de negócio)
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program
{
    protected Program() { }
}
