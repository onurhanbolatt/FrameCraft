# ============================================
# FrameCraft API - Multi-stage Dockerfile
# ============================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (better layer caching)
COPY ["FrameCraft.slnx", "."]
COPY ["src/FrameCraft.API/FrameCraft.API.csproj", "src/FrameCraft.API/"]
COPY ["src/FrameCraft.Application/FrameCraft.Application.csproj", "src/FrameCraft.Application/"]
COPY ["src/FrameCraft.Domain/FrameCraft.Domain.csproj", "src/FrameCraft.Domain/"]
COPY ["src/FrameCraft.Infrastructure/FrameCraft.Infrastructure.csproj", "src/FrameCraft.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/FrameCraft.API/FrameCraft.API.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/src/FrameCraft.API"
RUN dotnet build "FrameCraft.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "FrameCraft.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Create logs directory
RUN mkdir -p /var/log/framecraft && chown -R appuser:appuser /var/log/framecraft

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "FrameCraft.API.dll"]
