# Quick Start

## Docker (Simplest)

```bash
docker pull tasriyan/propel:latest

docker run -d \
  -p 8080:8080 \
  -e SQL_CONNECTION="Host=postgres;Database=propel;Username=user;Password=pass" \
  tasriyan/propel:latest
```

Access: **http://localhost:8080**  
Login: `admin` / `Admin123!`

## Docker Compose

```bash
# Get file
curl -O https://raw.githubusercontent.com/Treiben/propel-dashboard/main/docker-compose.release.yml

# Run (uses defaults)
docker-compose -f docker-compose.release.yml up -d
```

### With Redis

```bash
docker-compose -f docker-compose.release.yml --profile with-redis up -d
```

### Custom Config

```bash
# Create .env
cp .env.example .env

# Edit .env, then:
docker-compose -f docker-compose.release.yml up -d
```

## Verify

```bash
# Health check
curl http://localhost:8080/api/health/live

# Should return: Healthy
```

## Endpoints

- UI: http://localhost:8080
- API: http://localhost:8080/api
- Swagger: http://localhost:8080/swagger (if enabled)

## Next Steps

- Change default password
- Configure JWT secret (production)
- See [ENVIRONMENT-VARS.md](ENVIRONMENT-VARS.md) for all options
- See [DEPLOYMENT.md](DEPLOYMENT.md) for Kubernetes/cloud