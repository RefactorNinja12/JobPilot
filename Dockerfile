# Images for the two hosts. Build one with --target:
#   docker build --target worker -t jobpilot-worker .
#   docker build --target mcp    -t jobpilot-mcp .
# Both run as the non-root "app" user on chiseled Ubuntu (no shell, no package manager). The "-extra" variants
# include tzdata and ICU: JobTech times are converted with the Europe/Stockholm time zone.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Git hooks are for local development only (see Directory.Build.props).
ENV HUSKY=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
WORKDIR /src

# Restore first, from project files only, so the layer is reused until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Jobpilot.Domain/Jobpilot.Domain.csproj src/Jobpilot.Domain/
COPY src/Jobpilot.Application/Jobpilot.Application.csproj src/Jobpilot.Application/
COPY src/Jobpilot.Infrastructure/Jobpilot.Infrastructure.csproj src/Jobpilot.Infrastructure/
COPY src/Jobpilot.Worker/Jobpilot.Worker.csproj src/Jobpilot.Worker/
COPY src/Jobpilot.Mcp/Jobpilot.Mcp.csproj src/Jobpilot.Mcp/
RUN dotnet restore src/Jobpilot.Worker/Jobpilot.Worker.csproj \
 && dotnet restore src/Jobpilot.Mcp/Jobpilot.Mcp.csproj

COPY src/ src/
RUN dotnet publish src/Jobpilot.Worker/Jobpilot.Worker.csproj -c Release --no-restore -o /out/worker \
 && dotnet publish src/Jobpilot.Mcp/Jobpilot.Mcp.csproj -c Release --no-restore -o /out/mcp

FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra AS worker
WORKDIR /app
COPY --from=build /out/worker .
# ConnectionStrings__Jobpilot must be set; the Worker migrates the database at start.
ENTRYPOINT ["dotnet", "Jobpilot.Worker.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS mcp
WORKDIR /app
COPY --from=build /out/mcp .
# Listens on 8080 (ASPNETCORE_HTTP_PORTS default): /mcp for MCP, /health for probes.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Jobpilot.Mcp.dll"]
