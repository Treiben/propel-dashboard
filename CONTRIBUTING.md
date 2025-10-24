# Contributing to Propel Dashboard

Thank you for wanting to contribute to Propel Feature Flags Dashboard! This document explains how to report issues, run the project and tests locally, and prepare pull requests so your contribution is easy to review and land.

## Table of contents
- How to file an issue
- Feature requests & bug reports
- Development setup (prereqs)
- Build, test, and run (exact commands)
- Running the dashboard locally (dev & production-like)
- CI (GitHub Actions) — what runs in CI and how to reproduce locally
- Branching & commit guidelines
- Pull request checklist
- Code style and formatting
- Security reporting
- License & CLA

---

## How to file an issue
When opening an issue, pick the appropriate template (Bug or Feature) and include:
- A short, descriptive title
- A clear description of the problem or proposed change
- Steps to reproduce (for bugs)
- Expected vs actual behavior
- Environment: OS, Docker version, dotnet --version, Node version
- Any relevant logs, stack traces, or screenshots
- A minimal repro (gist or small repo) when possible

Before filing, search existing issues and discussions to reduce duplicates.

---

## Feature requests & bug reports
- For feature requests: explain the user story and the benefit to users.
- For bugs: include exact commands you ran and the output. If the bug is in the demo apps, include the endpoints called and sample payloads.

---

## Development setup (prereqs)
- Git
- Docker (Docker Desktop or engine) — required to run the development compose environment and for CI parity
- Make (optional) — Makefile is provided for convenience
- .NET 9 SDK (for dashboard-api)
  - Verify: dotnet --version (should be 9.x)
- Node 18+ / npm or pnpm (for dashboard-react)
  - Verify: node --version

Clone the repo and restore dependencies:

- git clone https://github.com/Treiben/propel-dashboard.git
- cd propel-dashboard

---

## Build, test, and run (exact commands)
The repository provides Makefile targets and direct commands. Use whichever you prefer; Make targets are used throughout the docs and CI.

1) Build production Docker image (Makefile):
   - make build
   - This runs: docker build -f dashboard-api/Dockerfile.production -t $(IMAGE_NAME):$(VERSION) .

2) Run the dev environment (hot reload / local development):
   - docker-compose up -d
   - API: http://localhost:5038
   - UI: http://localhost:3000

3) Run the release/prod compose (example):
   - docker-compose -f docker-compose.release.yml up -d

4) Run API build/publish manually (example):
   - cd dashboard-api
   - dotnet restore
   - dotnet build -c Release
   - dotnet publish -c Release -o ./publish /p:UseAppHost=false

5) Frontend (React / Vite):
   - cd dashboard-react
   - npm install
   - npm run dev    # local dev server
   - npm run build  # production build -> dist/

6) Run health checks / tests via Makefile:
   - make test
   - (The Makefile "test" target runs repository health/check scripts used in release checks.)

7) Useful Makefile targets:
   - make run           # Runs docker-compose.release.yml up -d
   - make logs          # View logs (see Makefile for exact command)
   - make clean         # Remove containers, volumes
   - make push          # Push built image to configured Docker registry

---

## Running the dashboard locally (dev & production-like)
- Development (with live reload):
  - docker-compose up -d
  - Wait for containers to be healthy. API accessible at http://localhost:5038, UI at http://localhost:3000
- Production-like (single container) using the production Dockerfile:
  - make build
  - make run (or docker-compose -f docker-compose.release.yml up -d)
  - The production image exposes port 8080 by default (see docker-compose.release.yml)

Seeding and migrations (when running standalone container):
- The Makefile and the release compose accept environment variables used by the container:
  - RUN_MIGRATIONS=Y to run auto-migrations at startup
  - SEED_DEFAULT_ADMIN=Y to create default admin credentials (change after first run)

Example run (standalone with SQL Server on host):

make run-standalone

or

docker run -d --name PROPEL-DASHBOARD -p 8080:8080 \
  -e SQL_CONNECTION="Server=host.docker.internal,1433;Database=PropelFeatureFlags;User Id=sa;Password=Propel_Password123!;TrustServerCertificate=True" \
  -e RUN_MIGRATIONS=Y \
  -e SEED_DEFAULT_ADMIN=Y \ 
  propel/feature-flags-dashboard:latest

---

## CI (GitHub Actions) — what runs in CI and how to reproduce locally
This repository uses GitHub Actions for CI. See .github/workflows/build.yml (exists in the repository) which is responsible for the build and release pipeline.

Typical CI steps (what CI runs):
- Checkout the repository
- Build the dashboard API (dotnet build / docker build using dashboard-api/Dockerfile.production)
- Build the frontend (npm install && npm run build)
- Run tests and health checks used by the Makefile (make test)
- Build multi-arch Docker images using docker buildx and push to Docker Hub and GitHub Packages on tagged releases
- Run security scan (Trivy) and publish scan results

To reproduce CI locally:
- Ensure Docker is running and you have Docker Buildx enabled
- Ensure you have .NET 9 SDK and Node installed
- Run the same steps in order:
  1. cd dashboard-react && npm install && npm run build
  2. cd ../dashboard-api && dotnet restore && dotnet build -c Release
  3. make build  # builds the production Docker image
  4. make test   # run health checks

Notes about publishing: the release process (tags) triggers the workflow to push images to Docker Hub and GitHub Packages and to run security scans. See docs/scratches/release_checklist.md for release steps used by maintainers.

---

## Branching & commits
- Create a feature branch from main:
  - git checkout -b feat/short-description
- Keep PRs small and focused
- Commit message style (recommended): follow Conventional Commits (feat:, fix:, docs:, chore:, refactor:)
  - Example: feat(api): add paginated flags endpoint
- Rebase or squash fixup commits as requested by maintainers before merge

---

## Pull request checklist
Before opening a PR, ensure:
- [ ] You have run the build and relevant tests locally (see Build & Test commands above)
- [ ] Frontend builds: cd dashboard-react && npm run build
- [ ] API builds: cd dashboard-api && dotnet build -c Release
- [ ] Docker image builds: make build
- [ ] Lint/formatting applied where applicable
- [ ] Add or update tests for new functionality where appropriate
- [ ] Update CHANGELOG.md (when applicable) and docs/ if behavior changes

In your PR description, include:
- What change you made and why
- How to test the change locally (commands and expected outcomes)
- Any relevant configuration or environment variables
- Link to related issues (e.g., Fixes #NNN)

---

## Code style and formatting
- Backend (.NET): follow repository .editorconfig and use dotnet format if you modify C# files
  - dotnet tool restore
  - dotnet format
- Frontend (React/TypeScript): follow the ESLint / Prettier setup in dashboard-react
  - npm run lint
  - npm run format (if available)

---

## Security reporting
Do not report security vulnerabilities in a public issue. Use one of these options:
- Repository security policy (if enabled): https://github.com/Treiben/propel-dashboard/security/policy
- Email a repository maintainer (see GitHub profile) or open a private security advisory

---

## License & Contributor License Agreement (CLA)
- Contributions are accepted under the repository's license (Apache-2.0). See LICENSE.
- If any CLA/DCO is required, it will be documented in the repository. By contributing, you agree to the project's contribution terms.

---

Thank you for contributing!