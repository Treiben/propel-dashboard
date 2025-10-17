# Quick Start

## Prerequisites

- Docker and Docker Compose installed
- Minimum 256MB RAM available (512MB recommended)

## Running with Docker Compose

### Option 1: Quick Start (Using Defaults)

Just run:
```bash
docker-compose -f docker-compose.release.yml up -d
```

This uses all defaults. The API will auto-generate a JWT secret (check logs for warning).

### Option 2: Custom Configuration

1. Copy the environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` to customize (all variables are optional):
   - Change database credentials
   - Set your own JWT_SECRET (recommended for production)
   - Change admin username/password
   - Configure ports

3. Start the dashboard:
   ```bash
   docker-compose -f docker-compose.release.yml up -d
   ```

4. Access the dashboard at `http://localhost:8080`
   - UI: `http://localhost:8080`
   - API: `http://localhost:8080/api`
   - Swagger: `http://localhost:8080/swagger` (if enabled)

5. Default credentials:
   - Username: `admin` (or value of `DEFAULT_ADMIN_USERNAME`)
   - Password: `changeme` (or value of `DEFAULT_ADMIN_PASSWORD`)

## Using Redis (Optional)

Redis provides distributed caching for feature flags to improve performance. When enabled:
- Feature flags are cached in Redis for faster evaluation
- The dashboard automatically invalidates cache when flags are updated
- **Important**: If your application caches flags in Redis, the dashboard must be connected to the same Redis instance to properly invalidate cached entries

To enable Redis caching:

1. Set in `.env`:
   ```bash
   ALLOW_FLAGS_UPDATE_IN_REDIS=true
   REDIS_CONNECTION=redis:6379
   ```

2. Start with Redis profile:
   ```bash
   docker-compose -f docker-compose.release.yml --profile with-redis up -d
   ```

## Using SQL Server Instead of PostgreSQL

1. Comment out the `postgres` service in `docker-compose.release.yml`
2. Uncomment the `sqlserver` service
3. Update `SQL_CONNECTION` environment variable to use SQL Server connection string
4. Update the `depends_on` section

## Stopping

```bash
docker-compose -f docker-compose.release.yml down
```

## Health Checks

- Liveness: `http://localhost:8080/api/health/live`
- Readiness: `http://localhost:8080/api/health/ready`