# Propel Feature Flags Dashboard - Architecture

## Production Architecture (Grafana-Style Single Container)

The production release uses a **monolithic container** approach, similar to Grafana:

```
┌─────────────────────────────────────────┐
│  propel/feature-flags-dashboard:latest  │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │   ASP.NET Core (Port 8080)        │ │
│  │                                   │ │
│  │   • API Endpoints (/api/*)        │ │
│  │   • Static File Serving (React)   │ │
│  │   • Health Checks                 │ │
│  │   • Swagger (optional)            │ │
│  └───────────────────────────────────┘ │
│                                         │
│  wwwroot/                               │
│  └── React Build (index.html, *.js)    │
└─────────────────────────────────────────┘
```

### Benefits
- ✅ Single image to version and release
- ✅ No inter-container networking
- ✅ Simpler deployment (`docker run -p 8080:8080`)
- ✅ Same-origin API calls (no CORS issues)
- ✅ Matches Grafana's successful pattern

### How It Works

1. **Build Process** (Multi-stage Dockerfile):
   - Stage 1: Build React app → static files in `dist/`
   - Stage 2: Build .NET API
   - Stage 3: Combine both into runtime image
   
2. **Runtime**:
   - ASP.NET Core serves static files from `wwwroot/`
   - API requests go to `/api/*`
   - All other routes fallback to `index.html` (SPA routing)

3. **Configuration**:
   - React built with `VITE_API_URL=/api` (same origin)
   - No nginx needed - ASP.NET Core handles everything

---

## Development Architecture (Separate Containers)

Development uses **separate containers** for hot reload:

```
┌─────────────────────┐      ┌──────────────────────┐
│  dashboard-api      │      │  dashboard-website   │
│  (ASP.NET Core)     │◄─────│  (Vite Dev Server)   │
│  Port 8080          │      │  Port 80             │
└─────────────────────┘      └──────────────────────┘
         │                            │
         │   docker network           │
         └────────────────────────────┘
```

### Benefits for Development
- ✅ Hot reload for React (instant changes)
- ✅ Hot reload for .NET (with volume mounts)
- ✅ Faster iteration
- ✅ Separate logs per service

### Files Used

**Production:**
- `dashboard-api/Dockerfile.production` - Unified build
- `docker-compose.release.yml` - Single service
- `dashboard-react/.env.production` - API at `/api`

**Development:**
- `dashboard-api/Dockerfile` - API only
- `dashboard-react/Dockerfile` - React dev server only
- `docker-compose.yml` + `docker-compose.override.yml` - Separate services
- `dashboard-react/.env.development` - API at `http://localhost:5038/api`

---

## Port Configuration

### Production
- **8080** - Single port for UI + API
  - `http://localhost:8080` → React UI
  - `http://localhost:8080/api` → API
  - `http://localhost:8080/swagger` → Swagger (if enabled)

### Development
- **8080** (container) / **5038** (host) - .NET API
- **80** (container) / **3000** (host) - React Dev Server
- **5432** - PostgreSQL
- **6379** - Redis (optional)

---

## Key Differences

| Aspect | Production | Development |
|--------|-----------|-------------|
| Containers | 1 (unified) | 2 (separate) |
| Hot Reload | ❌ | ✅ |
| Build Time | Slower (full build) | Faster (incremental) |
| Deployment | Simple | Complex |
| CORS | Not needed | Configured |
| Ports | 1 (8080) | 2 (5038, 3000) |

---

## Environment Variables

### Production (docker-compose.release.yml)
All configuration via environment variables:
- Database connection
- Redis (optional)
- JWT settings
- Admin credentials

### Development (appsettings.Development.json)
Configuration in files for convenience:
- Volume-mounted appsettings
- Hot reload enabled
- Debug logging
