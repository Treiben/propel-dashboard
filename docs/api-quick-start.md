# Propel Dashboard API - Quick Start Guide

## Running with Docker

### Minimal Setup (PostgreSQL)
```bash
docker run -d \
  --name propel-api \
  -p 8080:8080 \
  -e SQL_CONNECTION="Host=host.docker.internal;Port=5432;Database=propel;Username=postgres;Password=postgres" \
  yourusername/propel-dashboard-api:latest
```

### Minimal Setup (SQL Server)
```bash
docker run -d \
  --name propel-api \
  -p 8080:8080 \
  -e SQL_CONNECTION="Server=host.docker.internal,1433;Database=propel;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True" \
  yourusername/propel-dashboard-api:latest
```

## Using Docker Compose

### 1. Create `.env` file
```env
# Required
SQL_CONNECTION=Host=postgres;Port=5432;Database=propel_dashboard;Username=propel_user;Password=propel_password

# Recommended
JWT_SECRET=your-secure-secret-key-at-least-10-chars
```

### 2. Create `docker-compose.yml`
```yaml
version: '3.8'

services:
  api:
    image: yourusername/propel-dashboard-api:latest
    ports:
      - "8080:8080"
    env_file:
      - .env
    restart: unless-stopped

  # Optional: Include PostgreSQL
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: propel_dashboard
      POSTGRES_USER: propel_user
      POSTGRES_PASSWORD: propel_password
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### 3. Run
```bash
docker-compose up -d
```

## First Time Setup

### Option 1: Auto-create Admin User
```bash
docker run -d \
  -e SQL_CONNECTION="your-connection-string" \
  -e SEED_DEFAULT_ADMIN=true \
  -e DEFAULT_ADMIN_USERNAME=admin \
  -e DEFAULT_ADMIN_PASSWORD=MySecurePassword123! \
  yourusername/propel-dashboard-api:latest
```

The admin user will be forced to change password on first login.

### Option 2: Manual Admin Creation
Use your preferred database tool to insert an admin user after the API creates the tables.

## Verify Installation

### Check Health
```bash
curl http://localhost:8080/health/ready
```

### Access Swagger (if enabled)
```bash
# Enable Swagger
docker run -d \
  -e SQL_CONNECTION="your-connection-string" \
  -e ENABLE_SWAGGER=true \
  yourusername/propel-dashboard-api:latest
```

Visit: http://localhost:8080/swagger

## Production Configuration

### With Redis Cache
```bash
docker run -d \
  -e SQL_CONNECTION="your-connection-string" \
  -e JWT_SECRET="your-production-secret" \
  -e REDIS_CONNECTION="redis:6379" \
  -e CORS_ALLOWED_ORIGINS="https://dashboard.company.com" \
  yourusername/propel-dashboard-api:latest
```

### Full Production Example
```yaml
version: '3.8'

services:
  api:
    image: yourusername/propel-dashboard-api:latest
    ports:
      - "8080:8080"
    environment:
      # Required
      - SQL_CONNECTION=Host=db;Port=5432;Database=propel;Username=propel;Password=${DB_PASSWORD}
      
      # Security
      - JWT_SECRET=${JWT_SECRET}
      - CORS_ALLOWED_ORIGINS=https://dashboard.company.com
      
      # Performance
      - REDIS_CONNECTION=redis:6379
      
      # Disable auto-features
      - RUN_MIGRATIONS=false
      - SEED_DEFAULT_ADMIN=false
      - ENABLE_SWAGGER=false
    
    depends_on:
      - db
      - redis
    restart: always

  db:
    image: postgres:15
    environment:
      POSTGRES_DB: propel
      POSTGRES_USER: propel
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - db_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    command: redis-server --maxmemory 256mb --maxmemory-policy allkeys-lru

volumes:
  db_data:
```

## Troubleshooting

### Database Connection Failed
- Ensure database is accessible from container
- Use `host.docker.internal` for local databases
- Check firewall/network settings

### JWT Errors
- Set `JWT_SECRET` explicitly for production
- Use the same secret across all API instances

### CORS Issues
- Set `CORS_ALLOWED_ORIGINS` to your frontend URL
- For development, use `CORS_ALLOW_ALL=true`

### Health Check Failing
- Check database connection string
- Verify database has been migrated
- Check logs: `docker logs propel-api`