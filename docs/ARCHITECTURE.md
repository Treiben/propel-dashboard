# Architecture

## Production (Single Container)

```
┌─────────────────────────────────────────┐
│  tasriyan/propel:latest                 │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │   ASP.NET Core :8080              │  │
│  │   • API (/api/*)                  │  │
│  │   • Static Files (React)          │  │
│  │   • Health Checks                 │  │
│  └───────────────────────────────────┘  │
│                                         │
│  wwwroot/ (React build)                 │
└─────────────────────────────────────────┘
```

**Benefits:**
- Single image to deploy
- No CORS issues
- Simple networking
- Grafana-style approach

**How it works:**
1. Multi-stage Dockerfile builds React → static files
2. ASP.NET Core serves static files from `wwwroot/`
3. API at `/api/*`, SPA fallback to `index.html`
4. React built with `VITE_API_URL=/api` (same origin)

## Development (Separate Containers)

```
┌─────────────────────┐      ┌──────────────────────┐
│  api :5038          │◄─────│  ui :3000            │
│  (ASP.NET Core)     │      │  (Vite Dev Server)   │
└─────────────────────┘      └──────────────────────┘
```

**Benefits:**
- Hot reload for both
- Faster iteration
- Separate logs

**Files:**
- `dashboard-api/Dockerfile` - API only
- `dashboard-react/Dockerfile` - React dev server
- `docker-compose.yml` + `.override.yml` - Dev setup
- `.env.development` - API at `http://localhost:5038/api`

## Ports

**Production:**
- 8080 - UI + API

**Development:**
- 5038 (host) - API
- 3000 (host) - React dev
- 5432 - PostgreSQL
- 6379 - Redis (optional)

## Key Differences

| | Production | Development |
|---|---|---|
| Containers | 1 | 2 |
| Hot Reload | ❌ | ✅ |
| Build Time | Slower | Faster |
| CORS | Not needed | Configured |
| Ports | 1 | 2 |

## Environment Files

**Production:** `docker-compose.release.yml`
- All config via env vars
- `dashboard-api/Dockerfile.production`
- `.env.production` - API at `/api`

**Development:** `docker-compose.yml` + `.override.yml`
- Volume mounts for hot reload
- `appsettings.Development.json`
- `.env.development` - API at `http://localhost:5038/api`