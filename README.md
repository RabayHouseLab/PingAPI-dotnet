# RabayHouseLab.PingApi — Web API .NET 10

Web API minimalista em **ASP.NET Core 10** com endpoint `GET /ping` → `{"message":"pong"}`, em conformidade com os **Padrões para Microsoft .NET (SEBRAE/SP)**.

> Referências: [Project structure](https://learn.microsoft.com/pt-br/dotnet/core/porting/project-structure) · [Artifacts output](https://learn.microsoft.com/pt-br/dotnet/core/sdk/artifacts-output) · [ASP.NET Core 10](https://learn.microsoft.com/pt-br/aspnet/core/overview?view=aspnetcore-10.0) · [Unit testing + coverage](https://learn.microsoft.com/pt-br/dotnet/core/testing/unit-testing-code-coverage)

---

## Estrutura de diretórios

```text
RabayHouseLab.PingApi/                     <-- raiz do repositório Git
  RabayHouseLab.PingApi.slnx               <-- solution .slnx (XML, .NET 10)
  global.json                              # SDK 10.0.400 (rollForward: latestMajor)
  Directory.Build.props                    # UseArtifactsOutput, TreatWarningsAsErrors, AnalysisMode=Recommended
  .editorconfig                            # nomenclatura (_camelCase, usings fora do namespace)
  Dockerfile / .dockerignore / .gitignore
  src/
    RabayHouseLab.PingApi.Api/
      Controllers/PingController.cs
      Models/PingResponse.cs
      Data/ApplicationDbContext.cs         # EF Core (InMemory, pronto p/ SqlServer/Npgsql)
      Program.cs                           # AddDbContext + AddControllers + AddOpenApi + HealthChecks
      Properties/launchSettings.json
  tests/
    RabayHouseLab.PingApi.Tests/
      Controllers/PingControllerTests.cs
      Integration/PingEndpointTests.cs
  artifacts/                               <-- saída centralizada (UseArtifactsOutput)
```

### Convenções aplicadas

| Aspecto | Decisão |
|---|---|
| **Nomenclatura** | `[Empresa].[Produto].[Camada]` → `RabayHouseLab.PingApi.Api`, `RabayHouseLab.PingApi.Tests` |
| **Solution** | `RabayHouseLab.PingApi.slnx` (formato XML, padrão .NET 10) |
| **Namespaces** | `RabayHouseLab.PingApi.Api.Controllers`, `RabayHouseLab.PingApi.Api.Models`, etc. |
| **Framework** | `net10.0`, `Nullable` + `ImplicitUsings`, `UseArtifactsOutput` |
| **ORM** | EF Core 10.0.11 (`Microsoft.EntityFrameworkCore`, `.InMemory`, `.Design`) |
| **OpenAPI** | `Microsoft.AspNetCore.OpenApi` nativo via `AddOpenApi()` + `MapOpenApi()` |
| **Saúde** | `AddHealthChecks()` + `MapHealthChecks("/health")` |
| **Testes** | **xUnit.net** + `Microsoft.AspNetCore.Mvc.Testing` + `FluentAssertions`, `coverlet.collector`, `dotnet-coverage` |
| **Qualidade** | `TreatWarningsAsErrors`, `GenerateDocumentationFile`, `.editorconfig` |
| **Container** | Dockerfile multistage, `USER app`, `HEALTHCHECK` |

---

## Endpoints

| Método | Rota | Resposta |
|---|---|---|
| `GET` | `/ping` | `200 { "message": "pong" }` |
| `GET` | `/health` | `200 Healthy` |
| `GET` | `/openapi/v1.json` | OpenAPI (Development) |

## Execução

```bash
dotnet restore
dotnet build -c Release
dotnet test --collect:"XPlat Code Coverage"
dotnet run --project src/RabayHouseLab.PingApi.Api
```

## Docker

```bash
docker build -t pingapi:10 .
docker run -d -p 5142:8080 pingapi:10
curl -s http://localhost:5142/ping # {"message":"pong"}
```

## Cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
# relatórios em tests/RabayHouseLab.PingApi.Tests/TestResults/*/coverage.cobertura.xml
# alternativo: dotnet-coverage collect "dotnet test"
```
