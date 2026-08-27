# syntax=docker/dockerfile:1

# ──────────────────────────────────────────────────────────────
# Stage 1 — restore (cacheável separadamente)
# ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copia apenas arquivos de projeto para maximizar cache de restore
COPY RabayHouseLab.PingApi.slnx ./
COPY Directory.Build.props ./
COPY src/RabayHouseLab.PingApi.Api/RabayHouseLab.PingApi.Api.csproj src/RabayHouseLab.PingApi.Api/
COPY tests/RabayHouseLab.PingApi.Tests/RabayHouseLab.PingApi.Tests.csproj tests/RabayHouseLab.PingApi.Tests/

RUN dotnet restore --locked-mode 2> /dev/null || dotnet restore

# ──────────────────────────────────────────────────────────────
# Stage 2 — build & test (opcional quebrar em CI com --target test)
# ──────────────────────────────────────────────────────────────
FROM restore AS build
WORKDIR /src

# Copia o restante do código
COPY . .

# Build Release + validação (TreatWarningsAsErrors)
RUN dotnet build -c Release --no-restore

# ──────────────────────────────────────────────────────────────
# Stage 3 — test (pode ser alvo separado no CI: docker build --target test)
# ──────────────────────────────────────────────────────────────
FROM build AS test
RUN dotnet test -c Release --no-build --logger "console;verbosity=detailed"

# ──────────────────────────────────────────────────────────────
# Stage 4 — publish (trimmed, single-file opcional)
# ──────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish src/RabayHouseLab.PingApi.Api/RabayHouseLab.PingApi.Api.csproj \
    -c Release \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false

# ──────────────────────────────────────────────────────────────
# Stage 5 — runtime (imagem final mínima, non-root)
# ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Segurança: usuário não-root (app já existe na imagem base 8.0+, mas garantimos)
# A imagem aspnet 10.0 já vem com usuário 'app' (uid 1654). Usamos ele.
USER app

# Copia apenas o publish
COPY --from=publish /app/publish .

# Variáveis de ambiente de produção
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Healthcheck nativo do Docker (usa o endpoint /health mapeado no Program.cs)
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "RabayHouseLab.PingApi.Api.dll"]
