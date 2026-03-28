---
phase: 1
title: "Foundation — Docker, Infra, Shared Libraries"
status: completed
priority: P1
effort: 4h
depends_on: []
blocks: [2, 3, 4, 5]
completed: 2026-03-18
---

# Phase 1: Foundation

## Context Links
- [System Architecture](../../docs/system-architecture.md)
- [Codebase Summary](../../docs/codebase-summary.md)
- [Code Standards](../../docs/code-standards.md)

<!-- Updated: Validation Session 1 - TypeScript for hospital-shared-js; single root .gitignore covering .NET + Node.js + Docker -->

## Overview

Set up Docker Compose with all infrastructure services, create `.env.example`, single root `.gitignore` (covering .NET + Node.js + Docker), configure monitoring/logging infra, and scaffold both shared libraries (HospitalShared for .NET, hospital-shared-js for **TypeScript** Node.js).

## File Ownership

```
docker-compose.yml
.env.example
.gitignore
.editorconfig
infra/
  prometheus/prometheus.yml
  grafana/dashboards/          (empty placeholder)
  grafana/provisioning/
  scripts/init-databases.sh
shared/
  HospitalShared/
    HospitalShared.csproj
    DTOs/PatientDto.cs
    DTOs/AppointmentDto.cs
    DTOs/PrescriptionDto.cs
    Events/PatientCreatedEvent.cs
    Events/AppointmentScheduledEvent.cs
    Events/PrescriptionIssuedEvent.cs
    Events/InventoryLowEvent.cs
    Constants/EventTypes.cs
    Constants/ServiceUrls.cs
  hospital-shared-js/
    package.json
    tsconfig.json
    index.ts
    types/patient-types.ts
    types/appointment-types.ts
    types/event-types.ts
    utils/logger.ts
    utils/error-handler.ts
    utils/rabbitmq-client.js
    constants/event-names.js
```

## Implementation Steps

### Step 1: Root Config Files

**1.1 Create `.gitignore`**

```gitignore
# .NET
bin/
obj/
*.user
*.suo
appsettings.Development.json

# Node.js
node_modules/
dist/
.env
logs/

# IDE
.vs/
.vscode/
.idea/

# Docker
docker-compose.override.yml

# OS
Thumbs.db
.DS_Store
```

**1.2 Create `.editorconfig`**

```ini
root = true

[*]
indent_style = space
indent_size = 2
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
indent_size = 4
csharp_indent_case_contents = true
csharp_space_after_keywords_in_control_flow_statements = true

[*.{js,ts,json}]
indent_size = 2
```

**1.3 Create `.env.example`**

```bash
# === Gateway ===
GATEWAY_PORT=8000
JWT_ISSUER=http://keycloak:8080/realms/hospital
JWT_AUDIENCE=hospital-gateway

# === PostgreSQL ===
POSTGRES_USER=hospital
POSTGRES_PASSWORD=dev_password_change_me
POSTGRES_DB=hospital_db
POSTGRES_PORT=5432

# === MongoDB ===
MONGODB_URI=mongodb://mongo:27017/hospital
MONGO_PORT=27017

# === SQL Server ===
MSSQL_SA_PASSWORD=YourStrong!Passw0rd
MSSQL_SERVER=sqlserver
MSSQL_PORT=1433

# === RabbitMQ ===
RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_PORT=5672
RABBITMQ_MANAGEMENT_PORT=15672

# === Redis ===
REDIS_HOST=redis
REDIS_PORT=6379

# === Elasticsearch ===
ELASTICSEARCH_URI=http://elasticsearch:9200
ELASTICSEARCH_PORT=9200

# === Keycloak ===
KEYCLOAK_URL=http://keycloak:8080
KEYCLOAK_REALM=hospital
KEYCLOAK_CLIENT_ID=hospital-gateway
KEYCLOAK_CLIENT_SECRET=change_me_secret
KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=admin_password_change_me

# === Monitoring ===
SEQ_URL=http://seq:5341
PROMETHEUS_PORT=9090
GRAFANA_PORT=3000
GRAFANA_ADMIN_PASSWORD=admin

# === Service URLs (internal Docker network) ===
PATIENT_SERVICE_URL=http://patient-service:5001
APPOINTMENT_SERVICE_URL=http://appointment-service:5002
MEDICAL_RECORD_SERVICE_URL=http://medical-record-service:5003
PHARMACY_SERVICE_URL=http://pharmacy-service:5004
NOTIFICATION_SERVICE_URL=http://notification-service:5005
SEARCH_SERVICE_URL=http://search-service:5006
```

### Step 2: docker-compose.yml

Create `docker-compose.yml` with infrastructure services only (application services added by Phase 6).

**Services to include:**
- `postgres` — PostgreSQL 15, port 5432, volume `pgdata`, healthcheck via `pg_isready`
- `mongo` — MongoDB 6, port 27017, volume `mongodata`, healthcheck via `mongosh --eval`
- `sqlserver` — SQL Server 2022, port 1433, volume `sqldata`, healthcheck via `/opt/mssql-tools/bin/sqlcmd`
- `rabbitmq` — RabbitMQ 3.12 management, ports 5672+15672, volume `rabbitmqdata`, healthcheck via `rabbitmq-diagnostics`
- `redis` — Redis 7 Alpine, port 6379, healthcheck via `redis-cli ping`
- `elasticsearch` — Elasticsearch 8, port 9200, volume `esdata`, single-node, security disabled for dev
- `keycloak` — Keycloak 21, port 8080, depends_on sqlserver (uses SQL Server as DB or H2 for dev)
- `prometheus` — Prometheus 2.45, port 9090, mount `infra/prometheus/prometheus.yml`
- `grafana` — Grafana 10, port 3000, depends_on prometheus
- `seq` — Seq 2023.4, port 5341, accept EULA

**Network:** `hospital-network` (bridge)

**Volumes:** `pgdata`, `mongodata`, `sqldata`, `rabbitmqdata`, `esdata`, `redisdata`

**Key docker-compose patterns:**
```yaml
services:
  postgres:
    image: postgres:15-alpine
    container_name: hospital-postgres
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./infra/scripts/init-databases.sh:/docker-entrypoint-initdb.d/init.sh
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - hospital-network
```

Use env variable substitution (`${VAR}`) for all credentials. Each service must have `healthcheck`, `container_name`, `restart: unless-stopped`, and `networks: [hospital-network]`.

### Step 3: Infrastructure Configs

**3.1 `infra/prometheus/prometheus.yml`**

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'gateway'
    static_configs:
      - targets: ['hospital-gateway:8000']
  - job_name: 'patient-service'
    static_configs:
      - targets: ['patient-service:5001']
  - job_name: 'appointment-service'
    static_configs:
      - targets: ['appointment-service:5002']
```

**3.2 `infra/grafana/provisioning/datasources/datasource.yml`**

```yaml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
```

**3.3 `infra/scripts/init-databases.sh`**

Bash script that runs as PostgreSQL init:
- Creates `hospital_db` if not exists (handled by POSTGRES_DB env)
- Creates `patients` and `appointments` tables with proper schema
- Adds indexes on email, patient_id

### Step 4: Shared .NET Library (HospitalShared)

**4.1 Create project**

```bash
cd shared
dotnet new classlib -n HospitalShared --framework net8.0
```

**4.2 Add NuGet packages to `HospitalShared.csproj`**

```xml
<PackageReference Include="MassTransit" Version="8.1.*" />
```

**4.3 DTOs** (one file per DTO, ~30 lines each)

- `DTOs/PatientDto.cs` — Id, Email, FirstName, LastName, DateOfBirth, PhoneNumber, CreatedAt
- `DTOs/AppointmentDto.cs` — Id, PatientId, ProviderId, ScheduledTime, Duration, Status, Notes
- `DTOs/PrescriptionDto.cs` — Id, PatientId, DrugId, Quantity, Instructions, Status

**4.4 Events** (one file per event, ~15 lines each)

- `Events/PatientCreatedEvent.cs` — PatientId, Email, FirstName, LastName, Timestamp
- `Events/AppointmentScheduledEvent.cs` — AppointmentId, PatientId, ProviderId, ScheduledTime
- `Events/PrescriptionIssuedEvent.cs` — PrescriptionId, PatientId, DrugId, Quantity
- `Events/InventoryLowEvent.cs` — DrugId, DrugName, CurrentStock, MinimumStock

**4.5 Constants**

- `Constants/EventTypes.cs` — static class with const strings for event names
- `Constants/ServiceUrls.cs` — static class with service URL config keys

### Step 5: Shared Node.js Library (hospital-shared-js)

**5.1 `package.json`**

```json
{
  "name": "@hospital/shared",
  "version": "1.0.0",
  "main": "index.js",
  "files": ["types/", "utils/", "constants/", "index.js"]
}
```

**5.2 `types/event-types.js`** — JSDoc type definitions for all domain events
**5.3 `types/patient-types.js`** — JSDoc types for Patient, PatientDto
**5.4 `types/appointment-types.js`** — JSDoc types for Appointment, AppointmentDto
**5.5 `utils/logger.js`** — Winston logger factory, accepts service name param
**5.6 `utils/error-handler.js`** — Express error handler middleware (standardized error response format)
**5.7 `utils/rabbitmq-client.js`** — amqplib wrapper: connect, publish, subscribe, with retry logic
**5.8 `constants/event-names.js`** — Exported constants matching .NET EventTypes
**5.9 `index.js`** — Re-exports all modules

## Commands to Run

```bash
# Root configs
# (create files as specified above)

# .NET shared library
cd shared && dotnet new classlib -n HospitalShared --framework net8.0
cd HospitalShared && dotnet add package MassTransit --version 8.1.3

# Node.js shared library
cd shared/hospital-shared-js && npm init -y
npm install winston amqplib
npm install --save-dev jest
```

## Todo List

- [x] Create `.gitignore`
- [x] Create `.editorconfig`
- [x] Create `.env.example`
- [x] Create `docker-compose.yml` with all infra services
- [x] Create `infra/prometheus/prometheus.yml`
- [x] Create `infra/grafana/provisioning/datasources/datasource.yml`
- [x] Create `infra/scripts/init-databases.sh`
- [x] Scaffold `shared/HospitalShared/` with DTOs, Events, Constants
- [x] Scaffold `shared/hospital-shared-js/` with types, utils, constants
- [x] Verify `docker-compose up -d` starts all infra (no app services yet)
- [x] Verify `dotnet build shared/HospitalShared/` compiles

## Success Criteria

- `docker-compose config` validates without errors
- `docker-compose up -d` starts all 10 infra containers
- All containers report healthy within 60s
- PostgreSQL accepts connections on 5432
- MongoDB accepts connections on 27017
- RabbitMQ management UI accessible on 15672
- `dotnet build shared/HospitalShared/` succeeds
- `cd shared/hospital-shared-js && node -e "require('.')"` succeeds

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| SQL Server image large (~2GB), slow pull | Medium | Pull in background, document in README |
| Elasticsearch memory limits | Medium | Set `ES_JAVA_OPTS=-Xms512m -Xmx512m` in compose |
| Port conflicts on dev machines | High | Use env var overrides for all ports |
| Keycloak DB choice for dev | Low | Use H2 (embedded) for simplicity in dev |
