# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file
COPY *.sln ./

# Copy project files
COPY src/TeamHubConnect.Blazor/*.csproj ./src/TeamHubConnect.Blazor/
COPY src/TeamHubConnect.Shared/*.csproj ./src/TeamHubConnect.Shared/

# Restore packages
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish application
WORKDIR /app/src/TeamHubConnect.Blazor
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM nginx:alpine AS runtime

# Copy published application
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copy nginx configuration
COPY nginx-blazor.conf /etc/nginx/conf.d/default.conf

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]