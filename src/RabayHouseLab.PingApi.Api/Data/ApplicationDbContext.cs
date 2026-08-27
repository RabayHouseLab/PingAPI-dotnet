using Microsoft.EntityFrameworkCore;

namespace RabayHouseLab.PingApi.Api.Data;

/// <summary>
/// DbContext base da aplicação. Usa InMemory para o cenário atual (sem persistência externa),
/// mas expõe o ponto de extensão para provedores relacionais (SqlServer, Npgsql, etc.)
/// conforme padrão EF Core do documento de padrões.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
