# Environment Variables

All configuration via environment variables. Set explicitly in production.

## Required

**`SQL_CONNECTION`** - Database connection string  
Examples:
- PostgreSQL: `Host=postgres;Port=5432;Database=propel;Username=user;Password=pass`
- SQL Server: `Server=sqlserver,1433;Database=propel;User Id=sa;Password=pass;TrustServerCertificate=True`

## Optional

### JWT Configuration

**`JWT_SECRET`** - JWT signing key (min 10 chars)  
Default: Auto-generated 32-char string (⚠️ not persisted - set for production!)

**`JWT_ISSUER`** - JWT issuer  
Default: `propel-dashboard`

**`JWT_AUDIENCE`** - JWT audience  
Default: `propel-dashboard-api`

### Admin Account

**`SEED_DEFAULT_ADMIN`** - Create default admin on startup  
Default: `false`  
Values: `true`, `false`, `y`, `n`, `1`, `0`

**`DEFAULT_ADMIN_USERNAME`** - Default admin username  
Default: `admin`

**`DEFAULT_ADMIN_PASSWORD`** - Default admin password  
Default: `Admin123!`

### Redis Cache

**`ALLOW_FLAGS_UPDATE_IN_REDIS`** - Enable Redis cache updates  
Default: `false`  
Values: `true`, `false`, `y`, `n`, `1`, `0`

**`REDIS_CONNECTION`** - Redis connection string  
Default: Empty (required if `ALLOW_FLAGS_UPDATE_IN_REDIS=true`)  
Example: `redis:6379`

### Database

**`RUN_MIGRATIONS`** - Auto-run migrations on startup  
Default: `true`  
Values: `true`, `false`, `y`, `n`, `1`, `0`

## Docker Example

```bash
docker run -d \
  -p 8080:8080 \
  -e SQL_CONNECTION="Host=postgres;Database=propel;Username=user;Password=pass" \
  -e JWT_SECRET="$(openssl rand -base64 32)" \
  -e SEED_DEFAULT_ADMIN="true" \
  -e ALLOW_FLAGS_UPDATE_IN_REDIS="true" \
  -e REDIS_CONNECTION="redis:6379" \
  tasriyan/propel:latest
```

## Production Checklist

- ✅ Set `JWT_SECRET` explicitly
- ✅ Change default admin credentials
- ✅ Set `SEED_DEFAULT_ADMIN=false` after first run
- ✅ Use persistent volumes for database
- ✅ Connect to same Redis instance as your apps (if using cache)

## Health Endpoints

- `/api/health/live` - Liveness (is running?)
- `/api/health/ready` - Readiness (accepting traffic?)
- `/api/health` - All checks