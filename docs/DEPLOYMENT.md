# Deployment Guide

## Quick Start (Docker)

The fastest way to run Propel Feature Flags Dashboard:

```bash
docker run -d \
  -p 8080:8080 \
  -e SQL_CONNECTION="Host=your-postgres;Database=propel;Username=user;Password=pass" \
  propel/feature-flags-dashboard:latest
```

Access the dashboard at `http://localhost:8080`

---

## Docker Compose Deployment

### 1. Download Files

```bash
curl -O https://raw.githubusercontent.com/[your-org]/propel-dashboard/main/docker-compose.release.yml
curl -O https://raw.githubusercontent.com/[your-org]/propel-dashboard/main/.env.example
```

### 2. Configure

```bash
cp .env.example .env
# Edit .env with your settings (all optional - has defaults)
```

### 3. Run

```bash
# With PostgreSQL (default)
docker-compose -f docker-compose.release.yml up -d

# With Redis caching
ALLOW_FLAGS_UPDATE_IN_REDIS=true \
docker-compose -f docker-compose.release.yml --profile with-redis up -d
```

### 4. Access

- **Dashboard UI**: http://localhost:8080
- **API**: http://localhost:8080/api
- **Health Check**: http://localhost:8080/api/health/live
- **Default Credentials**: admin / changeme

---

## Environment Variables

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `SQL_CONNECTION` | Database connection string | `Host=postgres;Database=propel;Username=user;Password=pass` |

### Optional (with defaults)

| Variable | Default | Description |
|----------|---------|-------------|
| `JWT_SECRET` | Auto-generated | JWT signing key (set for production!) |
| `JWT_ISSUER` | `propel-dashboard` | JWT issuer |
| `JWT_AUDIENCE` | `propel-dashboard-api` | JWT audience |
| `DEFAULT_ADMIN_USERNAME` | `admin` | Initial admin username |
| `DEFAULT_ADMIN_PASSWORD` | `changeme` | Initial admin password |
| `ALLOW_FLAGS_UPDATE_IN_REDIS` | `false` | Enable Redis caching |
| `REDIS_CONNECTION` | `redis:6379` | Redis connection string |
| `RUN_MIGRATIONS` | `true` | Auto-run database migrations |
| `SEED_DEFAULT_ADMIN` | `true` | Create default admin user |
| `DASHBOARD_PORT` | `8080` | Port to expose dashboard |

---

## Production Best Practices

### 1. Set a JWT Secret

```bash
# Generate a secure secret
openssl rand -base64 32

# Set in .env
JWT_SECRET=your-generated-secret-here
```

### 2. Change Default Credentials

```bash
DEFAULT_ADMIN_USERNAME=myadmin
DEFAULT_ADMIN_PASSWORD=StrongPassword123!
```

### 3. Use Persistent Storage

The docker-compose.release.yml includes volumes for data persistence:
- `postgres_data` - Database data
- `redis_data` - Cache data (if using Redis)

### 4. Enable HTTPS

Use a reverse proxy (nginx, Traefik, Caddy) for HTTPS:

```nginx
server {
    listen 443 ssl;
    server_name dashboard.example.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 5. Configure CORS (if needed)

```bash
CORS_ALLOWED_ORIGINS=https://app.example.com,https://admin.example.com
CORS_ALLOW_ALL=false
```

### 6. Enable Redis Caching (Optional)

If your applications cache feature flags in Redis:

```bash
ALLOW_FLAGS_UPDATE_IN_REDIS=true
REDIS_CONNECTION=redis.example.com:6379
```

**Important:** Connect the dashboard to the same Redis instance your applications use. The dashboard automatically invalidates cached flags when updates are made, ensuring your applications always have current configuration.

---

## Kubernetes Deployment

Example Kubernetes manifests:

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: propel-dashboard
spec:
  replicas: 2
  selector:
    matchLabels:
      app: propel-dashboard
  template:
    metadata:
      labels:
        app: propel-dashboard
    spec:
      containers:
      - name: dashboard
        image: propel/feature-flags-dashboard:latest
        ports:
        - containerPort: 8080
        env:
        - name: SQL_CONNECTION
          valueFrom:
            secretKeyRef:
              name: propel-secrets
              key: sql-connection
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: propel-secrets
              key: jwt-secret
        livenessProbe:
          httpGet:
            path: /api/health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /api/health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
```

### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: propel-dashboard
spec:
  selector:
    app: propel-dashboard
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

---

## Database Options

### PostgreSQL (Recommended)

```bash
SQL_CONNECTION="Host=postgres;Port=5432;Database=propel_feature_flags;Username=propel_user;Password=your_password;Include Error Detail=true"
```

### SQL Server

```bash
SQL_CONNECTION="Server=sqlserver,1433;Database=PropelFeatureFlags;User Id=sa;Password=Your_Password123!;TrustServerCertificate=True"
```

---

## Monitoring

### Health Endpoints

- **Liveness**: `/health/live` - Container is running
- **Readiness**: `/health/ready` - Ready to accept traffic

### Prometheus Metrics

Add Prometheus endpoint (if implemented):

```yaml
scrape_configs:
  - job_name: 'propel-dashboard'
    static_configs:
      - targets: ['propel-dashboard:8080']
```

---

## Troubleshooting

### Container won't start

```bash
# Check logs
docker logs propel-dashboard

# Common issues:
# - Missing SQL_CONNECTION
# - Database not reachable
# - Port already in use
```

### Can't connect to database

```bash
# Test connection from container
docker exec -it propel-dashboard bash
curl http://postgres:5432
```

### UI not loading

```bash
# Check if static files are present
docker exec propel-dashboard ls -la /app/wwwroot

# Should see: index.html, assets/, etc.
```

### API works but UI doesn't

Check browser console for errors. Common issue:
- CORS misconfiguration (but shouldn't happen with single container)

---

## Upgrading

### Docker Compose

```bash
docker-compose -f docker-compose.release.yml pull
docker-compose -f docker-compose.release.yml up -d
```

### Docker

```bash
docker pull propel/feature-flags-dashboard:latest
docker stop propel-dashboard
docker rm propel-dashboard
# Run with same parameters as before
```

### Kubernetes

```bash
kubectl set image deployment/propel-dashboard \
  dashboard=propel/feature-flags-dashboard:v2.0.0
```

