# Use the official .NET 10 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/EMSCore.Edge/EMSCore.Edge.csproj", "src/EMSCore.Edge/"]
COPY ["src/EMSCore.Application/EMSCore.Application.csproj", "src/EMSCore.Application/"]
COPY ["src/EMSCore.Infrastructure/EMSCore.Infrastructure.csproj", "src/EMSCore.Infrastructure/"]
COPY ["src/EMSCore.Domain/EMSCore.Domain.csproj", "src/EMSCore.Domain/"]

# Restore dependencies
RUN dotnet restore "src/EMSCore.Edge/EMSCore.Edge.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/EMSCore.Edge"
RUN dotnet build "EMSCore.Edge.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EMSCore.Edge.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - create runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "EMSCore.Edge.dll"]