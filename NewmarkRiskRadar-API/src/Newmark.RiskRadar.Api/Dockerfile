# Build stage: restore against the project files first so dependency layers cache independently of source changes.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Newmark.RiskRadar.Domain/Newmark.RiskRadar.Domain.csproj src/Newmark.RiskRadar.Domain/
COPY src/Newmark.RiskRadar.Application/Newmark.RiskRadar.Application.csproj src/Newmark.RiskRadar.Application/
COPY src/Newmark.RiskRadar.Infrastructure/Newmark.RiskRadar.Infrastructure.csproj src/Newmark.RiskRadar.Infrastructure/
COPY src/Newmark.RiskRadar.Api/Newmark.RiskRadar.Api.csproj src/Newmark.RiskRadar.Api/
RUN dotnet restore src/Newmark.RiskRadar.Api/Newmark.RiskRadar.Api.csproj

COPY src/ src/
RUN dotnet publish src/Newmark.RiskRadar.Api/Newmark.RiskRadar.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__SqliteConnection="Data Source=/app/data/riskradar.db"

# Named volumes inherit ownership from the image directory, so chown before dropping privileges.
RUN mkdir -p /app/data && chown -R app:app /app/data

COPY --from=build /app/publish .

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Newmark.RiskRadar.Api.dll"]
