# PingApi — Web API .NET 10

Web API minimalista em **ASP.NET Core 10** com um único endpoint `GET /ping` que responde `{"message":"pong"}` em JSON estruturado, criada seguindo as **boas práticas da documentação oficial do .NET 10**.

> Referências: [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/), [ASP.NET Core OpenAPI (.NET 9/10)](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi), [Organização de projetos .NET](https://learn.microsoft.com/dotnet/core/porting/project-structure), [Testes com xUnit](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test), [Docker .NET best practices](https://learn.microsoft.com/dotnet/docker/building-net-docker-images)

---

## Estrutura de diretórios

```
PingApi.sln
global.json                    # trava SDK em 10.0.400 (rollForward: latestMajor)
Directory.Build.props          # propriedades compartilhadas (Nullable, ImplicitUsings, AnalysisMode=Recommended)
.editorconfig / .gitignore / .dockerignore
Dockerfile                     # multistage (restore → build → test → publish → runtime)
src/
  PingApi.Api/
    Controllers/PingController.cs   # [ApiController] + [Route("ping")] — único endpoint
    Models/PingResponse.cs          # sealed record imutável da resposta JSON
    Program.cs                      # AddControllers + AddOpenApi (nativo .NET 10) + HealthChecks
    Properties/launchSettings.json  # launchUrl: openapi/v1.json
    appsettings.json
    PingApi.Api.http               # requests para teste manual
    PingApi.Api.csproj             # net10.0, Microsoft.AspNetCore.OpenApi 10.0.11
tests/
  PingApi.Tests/
    Controllers/PingControllerTests.cs   # testes de unidade (xUnit + FluentAssertions)
    Integration/PingEndpointTests.cs     # testes de integração (WebApplicationFactory)
    PingApi.Tests.csproj                # net10.0, Microsoft.AspNetCore.Mvc.Testing 10.0.11
```

### Convenções aplicadas

| Aspecto | Decisão |
|---|---|
| **Nomenclatura** | Solution `PingApi`, projeto API `PingApi.Api`, projeto testes `PingApi.Tests` (PascalCase, sufixo por responsabilidade) |
| **Framework** | `net10.0` (via `global.json` 10.0.400), `Nullable` + `ImplicitUsings` habilitados |
| **OpenAPI** | `Microsoft.AspNetCore.OpenApi` nativo (.NET 10) via `AddOpenApi()` + `MapOpenApi()` — substitui Swashbuckle conforme template oficial `dotnet new webapi` no .NET 10 |
| **Controller** | `PingController : ControllerBase` com `[ApiController]` e `[Route("ping")]`, retorno `ActionResult<PingResponse>` |
| **Modelo** | `sealed record PingResponse(string Message)` — imutável |
| **Serialização JSON** | `System.Text.Json` com `PropertyNamingPolicy = CamelCase` → `{"message":"pong"}` |
| **Documentação** | `GenerateDocumentationFile` + comentários XML no controller/model + OpenAPI nativo |
| **Saúde** | `AddHealthChecks()` + `MapHealthChecks("/health")` (probe Kubernetes/LB + Docker HEALTHCHECK) |
| **Testes** | **xUnit.net** + `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 + `FluentAssertions` |
| **Qualidade** | `TreatWarningsAsErrors`, `AnalysisMode=Recommended`, `.editorconfig` |
| **Container** | Dockerfile multistage com cache de restore, usuário non-root `app`, `HEALTHCHECK`, `ASPNETCORE_URLS=http://+:8080` |

---

## Endpoints

| Método | Rota | Resposta | Descrição |
|---|---|---|---|
| `GET` | `/ping` | `200 { "message": "pong" }` | Health-check funcional |
| `GET` | `/health` | `200 Healthy` | Health-check da plataforma |
| `GET` | `/openapi/v1.json` | — | Documento OpenAPI (apenas `Development`) |

Exemplo:

```http
GET http://localhost:5142/ping
Accept: application/json
```
```json
{
  "message": "pong"
}
```

---

## Como executar (sem Docker)

### Pré-requisitos

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0) (o `global.json` já trava em 10.0.400)

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --logger "console;verbosity=detailed"
dotnet run --project src/PingApi.Api
# http://localhost:5142/ping → {"message":"pong"}
# http://localhost:5142/health
# http://localhost:5142/openapi/v1.json (Development)
```

Teste rápido:

```bash
curl -s http://localhost:5142/ping | jq
```

---

## Como executar com Docker

### Build da imagem (multistage)

O Dockerfile tem 5 stages: `restore` → `build` → `test` → `publish` → `final`.

```bash
# Build completo (inclui stage de publish, imagem final ~341 MB)
docker build -t pingapi:10 .

# Apenas validar testes dentro do Docker (sem gerar imagem final)
docker build --target test -t pingapi:10-test .

# Build com BuildKit (cache mais eficiente)
DOCKER_BUILDKIT=1 docker build -t pingapi:10 .
```

### Rodar o container

```bash
docker run -d --name pingapi -p 8080:8080 pingapi:10
# ou mapeando para outra porta:
docker run -d --name pingapi -p 5143:8080 pingapi:10

curl -s http://localhost:8080/ping   # {"message":"pong"}
curl -s http://localhost:8080/health # Healthy

docker logs pingapi
docker inspect pingapi --format '{{.Config.User}} {{.Config.Healthcheck.Test}}'
docker rm -f pingapi
```

### Docker Compose (opcional)

```yaml
services:
  api:
    build: .
    image: pingapi:10
    ports: ["8080:8080"]
    environment:
      ASPNETCORE_ENVIRONMENT: Production
```

### Boas práticas do Dockerfile aplicado

- **Cache de restore**: copia apenas `*.csproj` + `*.sln` antes de `dotnet restore`, aproveitando cache entre builds.
- **Multistage**: imagem final baseada em `mcr.microsoft.com/dotnet/aspnet:10.0` (sem SDK).
- **Non-root**: `USER app` (usuário `app` já existe na imagem base aspnet 10.0, uid 1654).
- **HEALTHCHECK**: `wget -qO- http://127.0.0.1:8080/health` com interval/timeout/retries.
- **Porta não-privilegiada**: `EXPOSE 8080` + `ASPNETCORE_URLS=http://+:8080`.
- **`.dockerignore`**: exclui `bin/`, `obj/`, `.git`, `TestResults`, etc., para contexto enxuto.
- **Publish otimizado**: `/p:UseAppHost=false` (sem executável nativo, roda via `dotnet PingApi.Api.dll`).

---

## Testes com cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Próximos passos sugeridos

- Adicionar `docker-compose.yml` + GitHub Actions com `docker build` + `dotnet test` + push para registry.
- Publicação AOT/trimmed (`PublishTrimmed`, `PublishAot`) quando compatível com o workload.
