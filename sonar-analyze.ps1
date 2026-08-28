#Requires -Version 5.1
<#
.SYNOPSIS
    Analise SonarQube via dotnet-sonarscanner — PingAPI (RabayHouseLab.PingApi)
.DESCRIPTION
    Sequencia completa e idempotente para analise local com a instancia
    em ../Sonarqube (http://localhost:9000).
    - Usa $env:SONAR_TOKEN (nao commitado) para autenticacao.
    - Assume dotnet-sonarscanner ja instalado (dotnet tool install --global dotnet-sonarscanner).
    - Aplica sonar.coverage.exclusions e sonar.cs.vstest.reportsPaths tambem via /d: no begin, pois Scanner for .NET 8+ nao le *.properties.
    - Cobre ./coverage/**/coverage.opencover.xml (OpenCover) + ./coverage/**/*.trx (vstest logger).
    - Trata automaticamente o conflito de sonar-project.cli.properties (renomeia temporariamente).
.PARAMETER SonarUrl
    URL do SonarQube. Default: http://localhost:9000
    Para execucao na rede sonarqube-net use http://sonarqube:9000, ou http://host.docker.internal:9000 no Windows/Mac sem rede compartilhada.
.PARAMETER Branch
    Nome do branch para analise (plugin mc1arke/community-branch-plugin). Ex.: feature/minha-feature
.PARAMETER PullRequestKey
    Chave numerica do PR. Se informado, PullRequestBranch e PullRequestBase sao obrigatorios.
.PARAMETER PullRequestBranch
    Branch origem do PR (cf. sonar.pullrequest.branch).
.PARAMETER PullRequestBase
    Branch base do PR (cf. sonar.pullrequest.base). Default: main quando PR informado.
.EXAMPLE
    $env:SONAR_TOKEN="sqp_xxx"
    ./sonar-analyze.ps1
.EXAMPLE
    $env:SONAR_TOKEN="sqp_xxx"
    ./sonar-analyze.ps1 -Branch feature/minha-feature
.EXAMPLE
    $env:SONAR_TOKEN="sqp_xxx"
    ./sonar-analyze.ps1 -PullRequestKey 42 -PullRequestBranch feature/minha-feature -PullRequestBase main
#>
[CmdletBinding()]
param(
    [string]$SonarUrl = "http://localhost:9000",
    [string]$Branch = "",
    [string]$PullRequestKey = "",
    [string]$PullRequestBranch = "",
    [string]$PullRequestBase = "main"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $env:SONAR_TOKEN) {
    Write-Error "SONAR_TOKEN nao definido. Gere em $SonarUrl -> My Account -> Security -> Generate Tokens e defina: `$env:SONAR_TOKEN='sqp_xxx'"
}

if ($PullRequestKey -and -not $PullRequestBranch) {
    Write-Error "PullRequestBranch e obrigatorio quando PullRequestKey e informado."
}

# dotnet-sonarscanner nao le *.properties — renomear temporariamente para evitar Post-processing failed
$cliProps = Join-Path $PSScriptRoot "sonar-project.cli.properties"
$cliPropsBak = "$cliProps.bak"
$renamed = $false
if (Test-Path $cliProps) {
    Move-Item -Force $cliProps $cliPropsBak
    $renamed = $true
}

try {
    Write-Host "Limpando .sonarqube e coverage (estado anterior)..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force (Join-Path $PSScriptRoot ".sonarqube"), (Join-Path $PSScriptRoot "coverage") -ErrorAction SilentlyContinue

    $commonArgs = @(
        "/k:RabayHouseLab.PingApi"
        "/d:sonar.host.url=$SonarUrl"
        "/d:sonar.token=$env:SONAR_TOKEN"
        "/d:sonar.cs.opencover.reportsPaths=coverage/**/coverage.opencover.xml"
        "/d:sonar.cs.vstest.reportsPaths=coverage/**/*.trx"
        "/d:sonar.coverage.exclusions=tests/**,**/*Tests/**,**/Program.cs,**/obj/**,**/bin/**,**/artifacts/**,**/*Designer.cs,**/*Generated*.cs,**/coverage/**"
    )

    if ($PullRequestKey) {
        $commonArgs += "/d:sonar.pullrequest.key=$PullRequestKey"
        $commonArgs += "/d:sonar.pullrequest.branch=$PullRequestBranch"
        $commonArgs += "/d:sonar.pullrequest.base=$PullRequestBase"
        $commonArgs += "/d:sonar.scm.revision=$(git rev-parse HEAD)"
        $commonArgs += "/d:sonar.pullrequest.provider=github"
        $commonArgs += "/d:sonar.pullrequest.github.repository=RabayHouseLab/PingAPI-dotnet"
    } elseif ($Branch) {
        $commonArgs += "/d:sonar.branch.name=$Branch"
    }

    Write-Host "dotnet sonarscanner begin..." -ForegroundColor Cyan
    & dotnet sonarscanner begin @commonArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet sonarscanner begin falhou (exit $LASTEXITCODE)" }

    Write-Host "dotnet build --no-incremental..." -ForegroundColor Cyan
    & dotnet build --no-incremental
    if ($LASTEXITCODE -ne 0) { throw "dotnet build falhou (exit $LASTEXITCODE)" }

    Write-Host "dotnet test --collect:`"XPlat Code Coverage`" --settings coverlet.runsettings + TRX..." -ForegroundColor Cyan
    & dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --logger "trx;LogFileName=test.trx" --results-directory ./coverage
    if ($LASTEXITCODE -ne 0) { throw "dotnet test falhou (exit $LASTEXITCODE)" }

    Write-Host "dotnet sonarscanner end..." -ForegroundColor Cyan
    & dotnet sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
    if ($LASTEXITCODE -ne 0) { throw "dotnet sonarscanner end falhou (exit $LASTEXITCODE)" }

    Write-Host "Analise concluida. Verifique $SonarUrl/dashboard?id=RabayHouseLab.PingApi" -ForegroundColor Green
    Write-Host "No log do end procure: Sensor C# Tests Coverage Report Import + Parsing the OpenCover report" -ForegroundColor DarkGray
}
finally {
    if ($renamed) {
        Move-Item -Force $cliPropsBak $cliProps -ErrorAction SilentlyContinue
    }
}
