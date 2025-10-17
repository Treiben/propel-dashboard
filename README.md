# Propel Feature Flags Dashboard

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Docker](https://img.shields.io/badge/docker-ready-brightgreen.svg)](https://hub.docker.com/r/propel/feature-flags-dashboard)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://reactjs.org/)

A centralized web dashboard for managing and configuring feature flags in your applications. Built with .NET 9 and React, deployed as a single Docker container for simplicity.

> **Note:** Looking for a CLI tool? Check out [Propel CLI](https://github.com/Treiben/propel-cli) for command-line flag management and database migrations in CI/CD pipelines.

---

## 🎬 Dashboard Demo

![Dashboard Overview](.github/assets/dashboard-demo.gif)

*Creating and configuring feature flags with targeting rules*

<details>
<summary>📸 More Screenshots</summary>

### Feature Flags List
![Flags List](.github/assets/screenshot-flags.png)

### Targeting Configuration
![Targeting Rules](.github/assets/screenshot-targeting.png)

### User Access Control
![User Management](.github/assets/screenshot-users.png)

</details>

---

## ✨ Features

- 🎯 **Feature Flag Management** - Create, configure, and manage feature flags with percentage rollouts, user targeting, and tenant-based access control
- 🔐 **JWT Authentication** - Secure access with role-based authorization
- 🎨 **Modern UI** - Built with React, TypeScript, and Vite for a responsive experience
- 🐳 **Single Container Deployment** - Grafana-style architecture - one image for both API and UI
- 💾 **Multi-Database Support** - Works with PostgreSQL or SQL Server
- ⚡ **Optional Redis Caching** - Distributed caching with automatic cache invalidation when flags are updated
- 🏥 **Health Checks** - Built-in liveness and readiness probes for orchestration
- 🔄 **Auto Migrations** - Database migrations run automatically on startup
- 🚀 **Production Ready** - Designed for enterprise deployment scenarios

---

## 🚀 Quick Start

### Using Docker (Recommended)

```bash
docker run -d \
  -p 8080:8080 \
  -e SQL_CONNECTION="Host=your-postgres;Database=propel;Username=user;Password=pass" \
  propel/feature-flags-dashboard:latest
```

Access the dashboard at **http://localhost:8080**

**Default credentials:** `admin` / `changeme`

### Using Docker Compose

1. Download the release compose file:
```bash
curl -O https://raw.githubusercontent.com/Treiben/propel-dashboard/main/docker-compose.release.yml
```

2. Start the stack:
```bash
docker-compose -f docker-compose.release.yml up -d
```

3. Access at **http://localhost:8080**


See [QUICKSTART.md](./docs/QUICKSTART.md) for details.

---

## 📋 Prerequisites

- **Docker** 20.10+ and **Docker Compose** 2.0+ (optional)
- **PostgreSQL** 12+ or **SQL Server** 2019+ (for production)
- **Redis** 7.0+ (optional, for distributed caching)
- **RAM**: 256MB minimum, 512MB recommended
- **Image Size**: ~276MB

---

## 🔧 Configuration

All configuration is done via environment variables:

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `SQL_CONNECTION` | Database connection string | `Host=postgres;Database=propel;Username=user;Password=pass` |

### Optional (with defaults)

| Variable | Default | Description |
|----------|---------|-------------|
| `JWT_SECRET` | Auto-generated | JWT signing key (⚠️ set for production!) |
| `JWT_ISSUER` | `propel-dashboard` | JWT issuer |
| `JWT_AUDIENCE` | `propel-dashboard-api` | JWT audience |
| `DEFAULT_ADMIN_USERNAME` | `admin` | Initial admin username |
| `DEFAULT_ADMIN_PASSWORD` | `changeme` | Initial admin password |
| `ALLOW_FLAGS_UPDATE_IN_REDIS` | `false` | Enable Redis caching |
| `REDIS_CONNECTION` | `redis:6379` | Redis connection string |
| `RUN_MIGRATIONS` | `true` | Auto-run database migrations |
| `SEED_DEFAULT_ADMIN` | `true` | Create default admin user |
| `DASHBOARD_PORT` | `8080` | Port to expose dashboard |

### Example with Environment Variables

```bash
docker run -d \
  --name propel-dashboard \
  -p 8080:8080 \
  -e SQL_CONNECTION="Server=myserver.database.windows.net;Database=Propel;User Id=admin;Password=SecurePass123!" \
  -e JWT_SECRET="your-32-character-minimum-secret-key" \
  -e DEFAULT_ADMIN_USERNAME="myadmin" \
  -e DEFAULT_ADMIN_PASSWORD="MySecurePass123!" \
  -e ALLOW_FLAGS_UPDATE_IN_REDIS=true \
  -e REDIS_CONNECTION="redis.example.com:6379" \
  propel/feature-flags-dashboard:latest
```

See [ENVIRONMENT.md](./docs/ENVIRONMENT-VARS.md) for details.

---

## 🏗️ Architecture

### Production (Single Container)

```
┌─────────────────────────────────────────┐
│  propel/feature-flags-dashboard:latest  │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │   ASP.NET Core (Port 8080)        │ │
│  │   • API Endpoints (/api/*)        │ │
│  │   • Static Files (React UI)       │ │
│  │   • Health Checks                 │ │
│  └───────────────────────────────────┘ │
│                                         │
│  wwwroot/ (React Build)                 │
└─────────────────────────────────────────┘
```

**Benefits:**
- ✅ Single image to version and deploy
- ✅ No inter-container networking complexity
- ✅ Same-origin API calls (no CORS issues)
- ✅ Simpler deployment and scaling

See [ARCHITECTURE.md](./docs/ARCHITECTURE.md) for detailed architecture documentation.

---

## 📦 Database Support

### PostgreSQL (Recommended)

```bash
SQL_CONNECTION="Host=postgres.example.com;Port=5432;Database=propel_flags;Username=propel_user;Password=SecurePass123"
```

### SQL Server

```bash
SQL_CONNECTION="Server=sqlserver.example.com,1433;Database=PropelFlags;User Id=propel_user;Password=SecurePass123!;TrustServerCertificate=True"
```

---

## ⚡ Redis Caching (Optional)

Enable Redis to cache feature flags for improved performance:

```bash
ALLOW_FLAGS_UPDATE_IN_REDIS=true
REDIS_CONNECTION=redis.example.com:6379
```

**Important:** If your applications cache feature flags in Redis, the dashboard must connect to the same Redis instance. When flags are updated through the dashboard, cached entries are automatically invalidated to ensure applications receive the latest configuration.

---

## 🔐 Security Best Practices

### 1. Set Strong JWT Secret

```bash
# Generate a secure secret
openssl rand -base64 32

# Use in environment variable
JWT_SECRET=your-generated-secret-here
```

### 2. Change Default Credentials

Always change the default admin credentials in production:

```bash
DEFAULT_ADMIN_USERNAME=myadmin
DEFAULT_ADMIN_PASSWORD=StrongPassword123!
```

### 3. Use HTTPS

Deploy behind a reverse proxy (nginx, Traefik, Caddy) for HTTPS:

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
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 🏥 Health Checks

| Endpoint | Purpose | Use Case |
|----------|---------|----------|
| `/health/live` | Liveness probe | Is container running? |
| `/health/ready` | Readiness probe | Ready for traffic? |
| `/health` | All health checks | General health status |

### Kubernetes Example

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
```

---

## 🛠️ Development

### Project Structure

```
propel-dashboard/
├── dashboard-api/           # .NET 9 API
│   ├── Dockerfile          # Development build
│   ├── Dockerfile.production  # Production build
│   ├── Program.cs
│   └── ...
├── dashboard-react/        # React + TypeScript UI
│   ├── src/
│   ├── vite.config.ts
│   └── ...
├── docker-compose.yml      # Development environment
├── docker-compose.release.yml  # Production release
├── Makefile               # Build commands
└── README.md
```

### Development Environment

```bash
# Start development environment (hot reload enabled)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

**Development URLs:**
- API: http://localhost:5038
- UI: http://localhost:3000
- PostgreSQL: localhost:5432
- Redis: localhost:6379

### Build Commands

```bash
# Build production image
make build

# Run with docker-compose
make run

# Run standalone (external database)
make run-standalone

# View logs
make logs

# Run health checks
make test

# Clean up
make clean
```

---

## 🚀 Deployment

See [DEPLOYMENT.md](./docs/DEPLOYMENT.md) for comprehensive deployment guides including:
- Kubernetes deployment
- Cloud platforms (Azure, AWS, GCP)
- Production configuration
- Monitoring and troubleshooting

---

## 📊 Versioning

We use [Semantic Versioning](https://semver.org/). Available versions can be found on:
- [Docker Hub](https://hub.docker.com/r/propel/feature-flags-dashboard)
- [GitHub Container Registry](https://github.com/Treiben/propel-dashboard/pkgs/container/feature-flags-dashboard)

**Tags:**
- `latest` - Latest stable release
- `v1.0.0` - Specific version
- `v1.0` - Latest patch of minor version
- `v1` - Latest minor of major version

---

## 🛠️ Related Tools

### Propel CLI

For command-line management and CI/CD integration:
- Manage feature flags from terminal
- Run database schema migrations
- Perfect for automation pipelines
- [View on GitHub](https://github.com/Treiben/propel-cli)

---

## 🤝 Contributing

Contributions are welcome! Please see our contributing guidelines.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

---

## 🆘 Support

- 📖 **Documentation**: [/docs](./docs)
- 🐛 **Issues**: [GitHub Issues](https://github.com/Treiben/propel-dashboard/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/orgs/Treiben/discussions)

---

## 🙏 Acknowledgments

Built with:
- [.NET 9](https://dotnet.microsoft.com/)
- [React](https://reactjs.org/)
- [Vite](https://vitejs.dev/)
- [PostgreSQL](https://www.postgresql.org/)
- [Redis](https://redis.io/)
- Inspired by [Grafana's](https://grafana.com/) single-container architecture

---

**Made with ❤️ by Tatyana Asriyan**