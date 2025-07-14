# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file
COPY *.sln ./

# Copy project files
COPY src/TeamHubConnect.Api/*.csproj ./src/TeamHubConnect.Api/
COPY src/TeamHubConnect.Application/*.csproj ./src/TeamHubConnect.Application/
COPY src/TeamHubConnect.Domain/*.csproj ./src/TeamHubConnect.Domain/
COPY src/TeamHubConnect.Infrastructure/*.csproj ./src/TeamHubConnect.Infrastructure/
COPY src/TeamHubConnect.Shared/*.csproj ./src/TeamHubConnect.Shared/

# Restore packages
RUN dotnet restore

# Copy source code
COPY . .

# Build application
WORKDIR /app/src/TeamHubConnect.Api
RUN dotnet build -c Release --no-restore

# Publish application
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "TeamHubConnect.Api.dll"]