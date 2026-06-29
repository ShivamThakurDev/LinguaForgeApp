# Build context is the repo root (see docker-compose.yml).
# Multi-stage: restore + publish on the SDK image, run on the slim ASP.NET runtime.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first for layer-cached restore.
COPY LinguaForge.API/LinguaForge.API.csproj LinguaForge.API/
COPY LinguaForge.Application/LinguaForge.Application.csproj LinguaForge.Application/
COPY LinguaForge.Domain/LinguaForge.Domain.csproj LinguaForge.Domain/
COPY LinguaForge.Infrastructure/LinguaForge.Infrastructure.csproj LinguaForge.Infrastructure/
RUN dotnet restore LinguaForge.API/LinguaForge.API.csproj

# Copy the rest of the source and publish.
COPY LinguaForge.API/ LinguaForge.API/
COPY LinguaForge.Application/ LinguaForge.Application/
COPY LinguaForge.Domain/ LinguaForge.Domain/
COPY LinguaForge.Infrastructure/ LinguaForge.Infrastructure/
RUN dotnet publish LinguaForge.API/LinguaForge.API.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Listen on plain HTTP inside the container; TLS is terminated by the platform/proxy.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Secrets (Jwt__Key, ConnectionStrings__DefaultConnection, Azure keys, Cors__AllowedOrigins__*)
# are supplied at runtime via environment variables — never baked into the image.
ENTRYPOINT ["dotnet", "LinguaForge.API.dll"]
