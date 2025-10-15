# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["dashboard-api/Propel.FeatureFlags.Dashboard.Api.csproj", "dashboard-api/"]
RUN dotnet restore "dashboard-api/Propel.FeatureFlags.Dashboard.Api.csproj"

# Copy source code and build
COPY dashboard-api/. dashboard-api/
WORKDIR /src/dashboard-api
RUN dotnet build "Propel.FeatureFlags.Dashboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Propel.FeatureFlags.Dashboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -g 1001 propel && useradd -u 1001 -g propel -m propel

# Copy published files
COPY --from=publish /app/publish .

# Set up environment
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Switch to non-root user
USER propel

EXPOSE 8080
ENTRYPOINT ["dotnet", "Propel.FeatureFlags.Dashboard.Api.dll"]