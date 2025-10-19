# Deployment

## Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: propel-dashboard
spec:
  replicas: 2
  selector:
    matchLabels:
      app: propel
  template:
    metadata:
      labels:
        app: propel
    spec:
      containers:
      - name: dashboard
        image: tasriyan/propel:latest
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
---
apiVersion: v1
kind: Service
metadata:
  name: propel-dashboard
spec:
  selector:
    app: propel
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

## HTTPS (nginx)

```nginx
server {
    listen 443 ssl http2;
    server_name dashboard.example.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Production Checklist

1. **Set JWT secret**
   ```bash
   JWT_SECRET=$(openssl rand -base64 32)
   ```

2. **Change admin credentials**
   ```bash
   DEFAULT_ADMIN_USERNAME=myadmin
   DEFAULT_ADMIN_PASSWORD=StrongPass123!
   ```

3. **Persistent storage**
   - Use volumes for postgres_data
   - Use volumes for redis_data (if using Redis)

4. **Connect to app's Redis**
   - If your apps cache flags in Redis, dashboard MUST use same instance
   - Dashboard auto-invalidates cache on flag updates

5. **Disable auto-seeding**
   ```bash
   SEED_DEFAULT_ADMIN=false  # After first run
   ```

## Database Options

**PostgreSQL (Recommended)**
```bash
SQL_CONNECTION="Host=postgres;Port=5432;Database=propel;Username=user;Password=pass"
```

**SQL Server**
```bash
SQL_CONNECTION="Server=sqlserver,1433;Database=propel;User Id=sa;Password=pass;TrustServerCertificate=True"
```

## Redis Setup

Enable if apps cache flags:
```bash
ALLOW_FLAGS_UPDATE_IN_REDIS=true
REDIS_CONNECTION=redis.example.com:6379
```

## Upgrading

```bash
# Docker Compose
docker-compose -f docker-compose.release.yml pull
docker-compose -f docker-compose.release.yml up -d

# Kubernetes
kubectl set image deployment/propel-dashboard dashboard=tasriyan/propel:v2.0.0
```

## Troubleshooting

**Container won't start**
```bash
docker logs propel-dashboard
# Check: SQL_CONNECTION set? Database reachable? Port available?
```

**Can't connect to DB**
```bash
docker exec -it propel-dashboard bash
curl http://postgres:5432
```

**UI not loading**
```bash
docker exec propel-dashboard ls /app/wwwroot
# Should see: index.html, assets/, etc.
```