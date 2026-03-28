---
phase: 6
title: "Integration — Docker Compose Wiring, Smoke Tests, Final Config"
status: completed
priority: P1
effort: 2h
depends_on: [2, 3, 4, 5]
blocks: []
completed: 2026-03-18
---

# Phase 6: Integration

## Context Links
- [System Architecture](../../docs/system-architecture.md)
- [Phase 1 — Foundation](./phase-01-foundation.md)
- [Phase 2 — Gateway](./phase-02-gateway-auth.md)
- [Phase 3 — .NET Services](./phase-03-dotnet-core-services.md)
- [Phase 4 — Node.js Part 1](./phase-04-nodejs-services-part1.md)
- [Phase 5 — Node.js Part 2](./phase-05-nodejs-services-part2.md)

## Overview

Wire all application services into docker-compose.yml, create docker-compose.override.yml for dev, set up smoke tests, and verify full stack boots with `docker-compose up -d`.

## File Ownership

```
docker-compose.yml              (APPEND service entries only — infra from Phase 1 untouched)
docker-compose.override.yml     (new file, dev overrides)
tests/
  smoke/
    smoke-test.sh
  integration/
    .gitkeep
  load/
    .gitkeep
```

**Important:** This phase only appends application service definitions to `docker-compose.yml`. Infrastructure services defined in Phase 1 remain unchanged.

## Implementation Steps

### Step 1: Add Application Services to docker-compose.yml

Append these service entries after the infrastructure services:

**1.1 hospital-gateway**

```yaml
  hospital-gateway:
    build:
      context: .
      dockerfile: gateway/HospitalGateway/Dockerfile
    container_name: hospital-gateway
    ports:
      - "${GATEWAY_PORT:-8000}:8000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8000
      - Keycloak__Authority=http://keycloak:8080/realms/hospital
      - Keycloak__Audience=hospital-gateway
      - Seq__Url=http://seq:5341
    depends_on:
      keycloak:
        condition: service_healthy
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 15s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
      - hospital-network
```

**1.2 patient-service**

```yaml
  patient-service:
    build:
      context: .
      dockerfile: services/PatientService/Dockerfile
    container_name: patient-service
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5001
      - ConnectionStrings__PostgreSQL=Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__User=${RABBITMQ_USER}
      - RabbitMQ__Password=${RABBITMQ_PASSWORD}
      - Seq__Url=http://seq:5341
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5001/health"]
      interval: 15s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
      - hospital-network
```

**1.3 appointment-service**

Same pattern as patient-service:
- Port: 5002
- Depends on: postgres, rabbitmq, patient-service (for HTTP calls)
- Additional env: `Services__PatientService=http://patient-service:5001`

**1.4 medical-record-service**

```yaml
  medical-record-service:
    build:
      context: .
      dockerfile: services/MedicalRecordService/Dockerfile
    container_name: medical-record-service
    ports:
      - "5003:5003"
    environment:
      - PORT=5003
      - MONGODB_URI=${MONGODB_URI}
      - RABBITMQ_HOST=rabbitmq
      - RABBITMQ_USER=${RABBITMQ_USER}
      - RABBITMQ_PASSWORD=${RABBITMQ_PASSWORD}
    depends_on:
      mongo:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "--spider", "-q", "http://localhost:5003/health"]
      interval: 15s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
      - hospital-network
```

**1.5 pharmacy-service**

- Port: 5004
- Depends on: sqlserver, rabbitmq
- Env: MSSQL_SERVER, MSSQL_SA_PASSWORD, MSSQL_DB=hospital_pharmacy

**1.6 notification-service**

- Port: 5005
- Depends on: redis, rabbitmq
- Env: REDIS_HOST, REDIS_PORT, RABBITMQ_*, SMTP_* (optional)

**1.7 search-service**

- Port: 5006
- Depends on: elasticsearch, rabbitmq
- Env: ELASTICSEARCH_URI, RABBITMQ_*

### Step 2: docker-compose.override.yml

Dev-only overrides for local development:

```yaml
version: '3.8'

services:
  # Expose debug ports for .NET services
  patient-service:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./services/PatientService:/src/services/PatientService

  appointment-service:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

  # Node.js services: mount source for hot reload
  medical-record-service:
    volumes:
      - ./services/MedicalRecordService/src:/app/src
    command: ["npx", "nodemon", "src/server.js"]

  pharmacy-service:
    volumes:
      - ./services/PharmacyService/src:/app/src
    command: ["npx", "nodemon", "src/server.js"]

  notification-service:
    volumes:
      - ./services/NotificationService/src:/app/src
    command: ["npx", "nodemon", "src/server.js"]

  search-service:
    volumes:
      - ./services/SearchService/src:/app/src
    command: ["npx", "nodemon", "src/server.js"]
```

### Step 3: Smoke Test Script

**`tests/smoke/smoke-test.sh`** (~60 lines)

```bash
#!/bin/bash
set -e

BASE_URL="http://localhost"
PASS=0
FAIL=0

check_health() {
  local name=$1
  local url=$2
  local status

  status=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")
  if [ "$status" = "200" ]; then
    echo "[PASS] $name ($url) -> $status"
    PASS=$((PASS + 1))
  else
    echo "[FAIL] $name ($url) -> $status"
    FAIL=$((FAIL + 1))
  fi
}

echo "=== Hospital HRM Smoke Tests ==="
echo ""

# Infrastructure
check_health "RabbitMQ Management" "$BASE_URL:15672"
check_health "Keycloak"           "$BASE_URL:8080"
check_health "Prometheus"         "$BASE_URL:9090/-/healthy"
check_health "Grafana"            "$BASE_URL:3000/api/health"
check_health "Seq"                "$BASE_URL:5341"

# Application services
check_health "Gateway"                  "$BASE_URL:8000/health"
check_health "Patient Service"          "$BASE_URL:5001/health"
check_health "Appointment Service"      "$BASE_URL:5002/health"
check_health "Medical Record Service"   "$BASE_URL:5003/health"
check_health "Pharmacy Service"         "$BASE_URL:5004/health"
check_health "Notification Service"     "$BASE_URL:5005/health"
check_health "Search Service"           "$BASE_URL:5006/health"

echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="

if [ "$FAIL" -gt 0 ]; then
  exit 1
fi
```

### Step 4: Create Placeholder Directories

```bash
mkdir -p tests/integration
mkdir -p tests/load
touch tests/integration/.gitkeep
touch tests/load/.gitkeep
```

### Step 5: Verify Full Stack

Run sequence:
1. `cp .env.example .env`
2. `docker-compose build`
3. `docker-compose up -d`
4. Wait 60s for all services to start
5. `bash tests/smoke/smoke-test.sh`
6. `docker-compose ps` — all containers should be "healthy"
7. `docker-compose down`

## Todo List

- [x] Append 7 application service entries to docker-compose.yml
- [x] Create docker-compose.override.yml with dev overrides
- [x] Create tests/smoke/smoke-test.sh
- [x] Create tests/integration/.gitkeep
- [x] Create tests/load/.gitkeep
- [x] Run `docker-compose config` to validate YAML
- [x] Run `docker-compose build` — all images build
- [x] Run `docker-compose up -d` — all containers start
- [x] Run smoke-test.sh — all health endpoints return 200
- [x] Verify docker-compose ps shows all containers healthy
- [x] Run `docker-compose down` to clean up

## Success Criteria

- `docker-compose config` validates without errors
- `docker-compose build` builds all 7 application images + uses pre-built infra images
- `docker-compose up -d` starts 17+ containers (10 infra + 7 app)
- All containers reach "healthy" status within 90s
- `tests/smoke/smoke-test.sh` passes (all 12 health checks return 200)
- `docker-compose down` cleanly stops all containers
- `docker-compose down -v` removes all volumes

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| Service startup order issues | High | Use depends_on with condition: service_healthy |
| Image build fails (missing files) | Medium | Build context is workspace root; verify COPY paths |
| Port conflicts on dev machine | Medium | All ports configurable via .env |
| Memory pressure (17+ containers) | High | Recommend 8GB+ RAM; document in README |
| Keycloak slow startup blocks gateway | Medium | Gateway health check retries; Keycloak has generous timeout |

## Security Considerations

- docker-compose.override.yml is in .gitignore (dev-only, not committed)
- All credentials come from .env (never hardcoded in compose file)
- Smoke test uses localhost only (no external network calls)
- No sensitive data in test scripts

## Next Steps After Phase 6

Once scaffold is verified:
1. Begin implementing real business logic per project-roadmap.md
2. Add EF Core migrations for .NET services
3. Add Mongoose/Sequelize seed data
4. Implement JWT auth middleware in all services
5. Wire up real RabbitMQ event handlers
6. Write unit tests targeting >80% coverage
