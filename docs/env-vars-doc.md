# Environment Variables Reference

This document lists all environment variables supported by the Propel Dashboard API.

## Required Variables

### `SQL_CONNECTION`
**Description**: Database connection string  
**Required**: Yes  
**Examples**:
- PostgreSQL: `Host=localhost;Port=5432;Database=propel;Username=user;Password=pass`
- SQL Server: `Server=localhost,1433;Database=propel;User Id=sa;Password=pass;TrustServerCertificate=True`

## Optional Variables

### Database Configuration

#### `DB_PROVIDER`
**Description**: Database provider type  
**Default**: Auto-detected from connection string  
**Values**: `postgres`, `sqlserver`  
**Note**: For future compatibility when more providers are added

#### `RUN_MIGRATIONS`
**Description**: Run database migrations on startup  
**Default**: `true`  
**Values**: `true`, `false`

### Authentication (JWT)

#### `JWT_SECRET`
**Description**: Secret key for JWT token signing  
**Default**: Auto-generated (32 characters)  
**Minimum Length**: 10 characters  
**Warning**: Auto-generated secrets are not persisted. Set explicitly for production.

#### `JWT_ISSUER`
**Description**: JWT token issuer  
**Default**: `propel-dashboard`

#### `JWT_AUDIENCE`  
**Description**: JWT token audience  
**Default**: `propel-dashboard-api`

### Default Admin Account

#### `SEED_DEFAULT_ADMIN`
**Description**: Create default admin account on startup  
**Default**: `false`  
**Values**: `true`, `false`

#### `DEFAULT_ADMIN_USERNAME`
**Description**: Username for default admin account  
**Default**: `admin`  
**Requires**: `SEED_DEFAULT_ADMIN=true`

#### `DEFAULT_ADMIN_PASSWORD`
**Description**: Password for default admin account  
**Default**: `admin1234!`  
**Requires**: `SEED_DEFAULT_ADMIN=true`  
**Note**: User will be forced to change password on first login

### Redis Cache

#### `REDIS_CONNECTION`
**Description**: Redis connection string  
**Default**: None (in-memory cache used)  
**Example**: `localhost:6379`

#### `CACHE_ENABLED`
**Description**: Enable distributed caching  
**Default**: `false`  
**Values**: `true`, `false`  
**Note**: Automatically set to `true` if `REDIS_CONNECTION` is provided

### CORS Configuration

#### `CORS_ALLOWED_ORIGINS`
**Description**: Comma-separated list of allowed origins  
**Default**: `http://localhost:3000,http://localhost:80`  
**Example**: `https://dashboard.company.com,https://app.company.com`

#### `CORS_ALLOW_ALL`
**Description**: Allow all origins (not recommended for production)  
**Default**: `false`  
**Values**: `true`, `false`

### Other Configuration

#### `ASPNETCORE_ENVIRONMENT`
**Description**: ASP.NET Core environment  
**Default**: `Production`  
**Values**: `Production`, `Development`, `Staging`

#### `ENABLE_SWAGGER`
**Description**: Enable Swagger UI  
**Default**: `false` (automatically `true` in Development)  
**Values**: `true`, `false`

## Docker Compose Example

```yaml
version: '3.8'
services:
  propel-api:
    image: yourusername/propel-dashboard-api:latest
    environment:
      # Required
      - SQL_CONNECTION=Host=db;Port=5432;Database=propel;Username=user;Password=pass
      
      # Recommended for production
      - JWT_SECRET=your-secure-secret-key-minimum-10-chars
      - CORS_ALLOWED_ORIGINS=https://dashboard.yourcompany.com
      
      # Optional
      - SEED_DEFAULT_ADMIN=true
      - REDIS_CONNECTION=redis:6379
      - ENABLE_SWAGGER=true
```

## Production Recommendations

1. **Always set `JWT_SECRET`** - Don't rely on auto-generated secrets
2. **Configure `CORS_ALLOWED_ORIGINS`** - Don't use `CORS_ALLOW_ALL` in production
3. **Use Redis for caching** - Better performance for multiple instances
4. **Set `RUN_MIGRATIONS=false`** - Run migrations separately in CI/CD
5. **Don't enable default admin** - Create admin accounts through proper channels

## Health Check Endpoints

The API exposes the following health check endpoints (no authentication required):

- `/health` - Overall health status
- `/health/live` - Liveness probe (is the app running?)
- `/health/ready` - Readiness probe (is the app ready to serve requests?)

## Running with Docker

### Quick Start
```bash
docker run -d \
  -p 8080:8080 \
  -e SQL_CONNECTION="your-connection-string" \
  yourusername/propel-dashboard-api:latest
```

### With All Options
```bash
docker run -d \
  -p 8080:8080 \
  -e SQL_CONNECTION="Host=localhost;Port=5432;Database=propel;Username=user;Password=pass" \
  -e JWT_SECRET="your-secure-secret-key" \
  -e CORS_ALLOWED_ORIGINS="https://dashboard.company.com" \
  -e REDIS_CONNECTION="redis:6379" \
  -e SEED_DEFAULT_ADMIN="false" \
  -e ENABLE_SWAGGER="true" \
  yourusername/propel-dashboard-api:latest
```