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
| **Testes** | **xUnit.net** + `Microsoft.AspNetCore.Mvc.Testing` + `coverlet.collector` (Assert nativo xUnit) |
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

Cobertura padronizada em **apenas OpenCover** via recursos nativos .NET (sem scripts):

- `coverlet.runsettings` → `<ResultsDirectory>./coverage</ResultsDirectory>` + `<Format>opencover</Format>` + `UseSourceLink=false`
- `tests/RabayHouseLab.PingApi.Tests/RabayHouseLab.PingApi.Tests.csproj` → `<VSTestResultsDirectory>../../coverage/</VSTestResultsDirectory>`

```bash
# Gera ./coverage/<guid>/coverage.opencover.xml (fullPath local C:\..., não URL)
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
# Limpar antes de nova análise (recomendado local):
# Remove-Item -Recurse -Force coverage -ErrorAction SilentlyContinue
# Exclusões: coverlet.runsettings ExcludeByFile=**/Program.cs (composition root)
#            + [ExcludeFromCodeCoverage] em Program.cs + sonar.coverage.exclusions=**/Program.cs
```

> `Program.cs` excluído via `ExcludeByFile` + `[ExcludeFromCodeCoverage]` (composition root — boa prática). `UseSourceLink=false` é obrigatório — com `true` o XML contém `https://raw.githubusercontent.com/...` e o Sonar não casa o arquivo. `GeneratedCodeAttribute` não é excluído (zera módulos Razor/gerados).

## Análise SonarQube (instância local `../Sonarqube`)

SonarQube Community com branch plugin em `http://localhost:9000` (`admin/admin`) — ver `../Sonarqube/README.md` e `../Sonarqube/docker-compose.yml` (imagem `mc1arke/sonarqube-with-community-branch-plugin:26.5.0` com `-javaagent` obrigatório).

### Pré-requisitos

```bash
# 1. Subir SonarQube (uma vez)
cd ../Sonarqube
docker compose up -d
# aguardar ~90s: curl http://localhost:9000/api/system/status  -> UP
# Browser: http://localhost:9000  (admin/admin, trocar senha no 1º login)

# 2. Criar projeto + token (uma vez)
# http://localhost:9000 → Create Project → Manually → Key: RabayHouseLab.PingApi
# My Account → Security → Generate Tokens → copie sqp_...

# 3. Scanner .NET (uma vez)
dotnet tool install --global dotnet-sonarscanner
```

### Análise via `dotnet-sonarscanner` (Scanner for .NET — único para C#)

> `Scanner for .NET 8+` **não lê** `sonar-project*.properties`. Se `sonar-project.cli.properties` existir na árvore o `end` falha (`Post-processing failed`). Renomeie temporariamente.
>
> **Script pronto:** `./sonar-analyze.ps1` na raiz (usa `$env:SONAR_TOKEN` e assume `dotnet-sonarscanner` já instalado). Veja `Obter ajuda: Get-Help ./sonar-analyze.ps1`.

```powershell
# Execucao simples (local):
$env:SONAR_TOKEN="sqp_XXXXXXXXXXXXXXXX"
./sonar-analyze.ps1

# Branch / PR:
./sonar-analyze.ps1 -Branch feature/minha-feature
./sonar-analyze.ps1 -PullRequestKey 42 -PullRequestBranch feature/minha-feature -PullRequestBase main

# Equivalente manual:
cd PingAPI
Rename-Item sonar-project.cli.properties sonar-project.cli.properties.bak -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .sonarqube,coverage -ErrorAction SilentlyContinue
$env:SONAR_TOKEN="sqp_XXXXXXXXXXXXXXXX"
dotnet sonarscanner begin /k:"RabayHouseLab.PingApi" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$env:SONAR_TOKEN" /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml" /d:sonar.cs.vstest.reportsPaths="coverage/**/*.trx" /d:sonar.coverage.exclusions="tests/**,**/*Tests/**,**/Program.cs,**/obj/**,**/bin/**,**/artifacts/**,**/*Designer.cs,**/*Generated*.cs,**/coverage/**"
dotnet build --no-incremental
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --logger "trx;LogFileName=test.trx" --results-directory ./coverage
dotnet sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
Rename-Item sonar-project.cli.properties.bak sonar-project.cli.properties -ErrorAction SilentlyContinue
# Ver: http://localhost:9000/dashboard?id=RabayHouseLab.PingApi
# Log do end deve conter: Sensor C# Tests Coverage Report Import + Parsing the OpenCover report
```

Branch / PR (plugin `mc1arke` — não misture `sonar.branch.*` com `sonar.pullrequest.*`):

```powershell
# Branch
dotnet sonarscanner begin /k:"RabayHouseLab.PingApi" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$env:SONAR_TOKEN" /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml" /d:sonar.cs.vstest.reportsPaths="coverage/**/*.trx" /d:sonar.coverage.exclusions="tests/**,**/*Tests/**,**/Program.cs,**/obj/**,**/bin/**,**/artifacts/**,**/*Designer.cs,**/*Generated*.cs,**/coverage/**" /d:sonar.branch.name="feature/minha-feature"
# PR
dotnet sonarscanner begin /k:"RabayHouseLab.PingApi" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$env:SONAR_TOKEN" /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml" /d:sonar.cs.vstest.reportsPaths="coverage/**/*.trx" /d:sonar.coverage.exclusions="tests/**,**/*Tests/**,**/Program.cs,**/obj/**,**/bin/**,**/artifacts/**,**/*Designer.cs,**/*Generated*.cs,**/coverage/**" /d:sonar.pullrequest.key="42" /d:sonar.pullrequest.branch="feature/minha-feature" /d:sonar.pullrequest.base="main" /d:sonar.scm.revision="$(git rev-parse HEAD)"
```

> **Não use** `/d:sonar.coverageReportPaths` com `**` no Windows — `GenericCoverageSensor` falha `Bad pathname` (`WinNTFileSystem.canonicalize`). Use apenas `sonar.cs.opencover.reportsPaths`. `sonar.coverage.exclusions` já contém `Program.cs` no `sonar-project.cli.properties`, mas o `dotnet-sonarscanner` exige `/d:sonar.coverage.exclusions` no `begin` (não lê `*.properties`). `Program.cs` é composition root com `[ExcludeFromCodeCoverage]` — boa prática.

