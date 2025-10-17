# Release Checklist

## Files to Add/Update

### ✅ New Files to Add
- [ ] `dashboard-api/Dockerfile.production` - Unified multi-stage build
- [ ] `dashboard-react/.env.production` - Production environment config
- [ ] `docker-compose.release.yml` - Release compose file
- [ ] `.env.example` - Environment variables template
- [ ] `.dockerignore` - Docker build optimization
- [ ] `Makefile` - Convenience commands
- [ ] `QUICKSTART.md` - User getting started guide
- [ ] `ARCHITECTURE.md` - System architecture documentation
- [ ] `DEPLOYMENT.md` - Deployment guide
- [ ] `.github/workflows/build.yml` - CI/CD pipeline

### ✅ Files to Update
- [ ] `dashboard-react/vite.config.ts` - Production build config
- [ ] `dashboard-api/Program.cs` - Add static file serving
- [ ] `dashboard-api/PropelConfiguration.cs` - Fix typo on line 54

### 📌 Files to Keep (Development Only)
- [ ] `dashboard-api/Dockerfile` - Dev API build
- [ ] `dashboard-react/Dockerfile` - Dev frontend with hot reload
- [ ] `docker-compose.yml` - Dev services
- [ ] `docker-compose.override.yml` - Dev overrides
- [ ] `dashboard-react/.env.development` - Dev environment

---

## GitHub Actions Setup

### Required Secrets
Configure these in GitHub Repository Settings → Secrets and variables → Actions:

- [ ] `DOCKERHUB_USERNAME` - Your Docker Hub username
- [ ] `DOCKERHUB_TOKEN` - Docker Hub access token (create at hub.docker.com/settings/security)

Note: `GITHUB_TOKEN` is automatically provided by GitHub Actions

---

## Testing Before Release

### 1. Test Production Build Locally

```bash
# Build the image
docker build -f dashboard-api/Dockerfile.production -t propel/feature-flags-dashboard:test .

# Run it
docker run -d -p 8080:8080 \
  -e SQL_CONNECTION="Host=host.docker.internal;Port=5432;Database=propel;Username=user;Password=pass" \
  propel/feature-flags-dashboard:test

# Test
curl http://localhost:8080/health/live
# Should return HTTP 200

# Access UI
open http://localhost:8080
# Should load React app

# Access API
curl http://localhost:8080/api/
# Should return API response
```

### 2. Test with Docker Compose

```bash
# Test the full stack
docker-compose -f docker-compose.release.yml up -d

# Check logs
docker-compose -f docker-compose.release.yml logs -f

# Verify health
make test  # or curl http://localhost:8080/health/live

# Clean up
docker-compose -f docker-compose.release.yml down -v
```

### 3. Test Development Still Works

```bash
# Ensure dev environment not broken
docker-compose up -d

# Should start separate API and frontend containers
docker ps | grep propel
# Should see: propel-dashboard-api and propel-dashboard-website

# Clean up
docker-compose down
```

---

## Release Process

### 1. Prepare Release

- [ ] Update version in package.json (if applicable)
- [ ] Update CHANGELOG.md
- [ ] Test all scenarios above
- [ ] Update documentation with any new features

### 2. Create Git Tag

```bash
# Create annotated tag
git tag -a v1.0.0 -m "Release v1.0.0"

# Push tag to trigger GitHub Actions
git push origin v1.0.0
```

### 3. GitHub Actions Will:

- [ ] Build multi-platform image (amd64, arm64)
- [ ] Push to Docker Hub: `propel/feature-flags-dashboard:v1.0.0`, `propel/feature-flags-dashboard:latest`
- [ ] Push to GitHub Packages: `ghcr.io/[owner]/feature-flags-dashboard:v1.0.0`
- [ ] Run security scan (Trivy)
- [ ] Upload security results to GitHub Security

### 4. Verify Release

```bash
# Pull from Docker Hub
docker pull propel/feature-flags-dashboard:v1.0.0

# Pull from GitHub Packages
docker pull ghcr.io/[owner]/feature-flags-dashboard:v1.0.0

# Test the released image
docker run -d -p 8080:8080 \
  -e SQL_CONNECTION="..." \
  propel/feature-flags-dashboard:v1.0.0
```

### 5. Announce Release

- [ ] Create GitHub Release with release notes
- [ ] Update README.md with latest version
- [ ] Update documentation site
- [ ] Announce in community channels

---

## Common Issues

### Build fails: "COPY failed"
- Check file paths in Dockerfile.production
- Ensure dashboard-react/ exists at root level

### Frontend not loading
- Check browser console for errors
- Verify `dist/` was created during build
- Check that static files copied to `wwwroot/`

### API calls fail from UI
- UI should use `/api` (relative path)
- Check `.env.production` has `VITE_API_URL=/api`

### Multi-platform build fails
- Ensure Docker Buildx is installed
- May need to run: `docker buildx create --use`

---

## Post-Release

### Monitor
- [ ] Check Docker Hub download stats
- [ ] Monitor GitHub Issues for release feedback
- [ ] Check security scan results

### Documentation
- [ ] Update getting started guide
- [ ] Add troubleshooting tips based on user feedback
- [ ] Create video tutorial (optional)

### Next Release
- [ ] Plan next features
- [ ] Address user feedback
- [ ] Update roadmap

---

## Rollback Plan

If issues found after release:

```bash
# Revert to previous version
docker pull propel/feature-flags-dashboard:v0.9.0

# Or revert git tag
git tag -d v1.0.0
git push origin :refs/tags/v1.0.0
```
