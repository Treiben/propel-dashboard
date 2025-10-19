.PHONY: help build push run stop clean test

LOCAL_IMAGE_NAME := propel/feature-flags-dashboard
PUSHED_IMAGE_NAME := tasriyan/propel

VERSION ?= latest
DOCKERFILE := dashboard-api/Dockerfile.production
CONTAINER_NAME := PROPEL-DASHBOARD

DEFAULT_SQL_SERVER_CONNECTION := "Server=host.docker.internal,1433;Database=PropelFeatureFlags;User Id=sa;Password=Propel_Password123!;TrustServerCertificate=True"
DEFAULT_POSTGRES_CONNECTION := "Host=host.docker.internal;Port=5432;Database=propel_feature_flags;Search Path=development;Username=propel_user;Password=propel_password"

SQL_SERVER_SEEDS_PATH := ".\seeds\sqlserver"
POSTGRES_SEEDS_PATH := ".\seeds\postgres"

ACTIVE_SQL_CONNECTION ?= $(DEFAULT_SQL_SERVER_CONNECTION)
ACTIVE_SEEDS_PATH := $(SQL_SERVER_SEEDS_PATH)
IMAGE_NAME := $(PUSHED_IMAGE_NAME)

help:
	@echo "Available targets:"
	@echo "  build       - Build the Docker image"
	@echo "  push        - Push image to Docker Hub"
	@echo "  run         - Run using docker-compose.release.yml"
	@echo "  stop        - Stop running containers"
	@echo "  clean       - Remove containers and volumes"
	@echo "  test        - Run health check tests"

build:
	docker build -f $(DOCKERFILE) -t $(IMAGE_NAME):$(VERSION) .

push:
	docker push $(IMAGE_NAME):$(VERSION)

run:
	docker-compose -f docker-compose.release.yml up -d

run-standalone:
	@echo "Running dashboard with external database..."
	docker run -d \
		--name $(CONTAINER_NAME) \
		-p 8080:8080 \
		-e SQL_CONNECTION=$(ACTIVE_SQL_CONNECTION) \
		-e ALLOW_FLAGS_UPDATE_IN_REDIS=N \
		-e RUN_MIGRATIONS=Y \
		-e SEED_DEFAULT_ADMIN=Y \
		-e DEFAULT_ADMIN_USERNAME=admin \
		-e DEFAULT_ADMIN_PASSWORD=Admin123! \
		$(IMAGE_NAME):$(VERSION)
	@echo "Dashboard started at http://localhost:8080"
	@echo "Container name: $(CONTAINER_NAME)"

run-with-redis:
	docker-compose -f docker-compose.release.yml --profile with-redis up -d

stop:
	@echo "Stopping containers..."
	@docker stop $(CONTAINER_NAME) 2>/dev/null || true
	docker-compose -f docker-compose.release.yml down

clean:
	@echo "Removing containers and volumes..."
	@docker rm -f $(CONTAINER_NAME) 2>/dev/null || true
	docker-compose -f docker-compose.release.yml down -v

test:
	@echo "Waiting for dashboard to be ready..."
	@timeout 30 sh -c 'until curl -f http://localhost:8080/health/live > /dev/null 2>&1; do sleep 1; done'
	@echo "✓ Dashboard is healthy"
	@curl -f http://localhost:8080/health/ready
	@echo "\n✓ All health checks passed"

logs:
	@docker logs -f $(CONTAINER_NAME) 2>/dev/null || docker-compose -f docker-compose.release.yml logs -f

migrate:
	@echo "Running database migrations..."
	@propel-cli migrate --connection-string $(ACTIVE_SQL_CONNECTION)
	@echo "Migrations completed."

seed-data:
	@echo "Adding sample data to the dashboard..."
	@propel-cli seed --connection-string $(ACTIVE_SQL_CONNECTION) --seeds-path $(ACTIVE_SEEDS_PATH)
	@echo "Sample data added."