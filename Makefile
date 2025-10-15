.PHONY: help build push run stop clean test

IMAGE_NAME := propel/feature-flags-dashboard
VERSION ?= latest
DOCKERFILE := dashboard-api/Dockerfile.production

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

run-with-redis:
	docker-compose -f docker-compose.release.yml --profile with-redis up -d

stop:
	docker-compose -f docker-compose.release.yml down

clean:
	docker-compose -f docker-compose.release.yml down -v

test:
	@echo "Waiting for dashboard to be ready..."
	@timeout 30 sh -c 'until curl -f http://localhost:8080/health/live > /dev/null 2>&1; do sleep 1; done'
	@echo "✓ Dashboard is healthy"
	@curl -f http://localhost:8080/health/ready
	@echo "\n✓ All health checks passed"