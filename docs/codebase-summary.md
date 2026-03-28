# Hospital HRM Microservices — Codebase Summary

**Current Status:** Scaffold Complete — All 7 microservices + gateway fully scaffolded and buildable (dotnet build & tsc verified).

---

## Directory Structure (Implemented)

```
hospital-microservices/
├── docker-compose.yml              # Orchestration for all services + infra
├── docker-compose.override.yml     # Development overrides (debug, logging)
├── .env                            # Environment variables (not in git)
├── .env.example                    # Template for .env
│
├── client/
│   ├── patient-app/                # Next.js 14 patient portal
│   │   ├── app/
│   │   │   ├── (dashboard)/        # Dashboard layout group
│   │   │   │   ├── page.tsx        # Dashboard overview
│   │   │   │   ├── appointments/   # Appointments pages
│   │   │   │   ├── medical-records/# Records pages
│   │   │   │   ├── prescriptions/  # Prescriptions pages
│   │   │   │   ├── profile/        # Profile pages
│   │   │   │   └── providers/      # Provider directory
│   │   │   └── api/
│   │   │       ├── auth/[...nextauth]/route.ts
│   │   │       ├── auth/token/route.ts
│   │   │       └── proxy/[...path]/route.ts
│   │   ├── components/             # React components
│   │   │   ├── dashboard/
│   │   │   ├── shared/
│   │   │   └── ui/ (shadcn/ui)
│   │   ├── lib/
│   │   │   ├── auth-config.ts      # NextAuth + Keycloak
│   │   │   ├── auth-session.ts     # Token management
│   │   │   ├── hooks/              # Custom React hooks
│   │   │   ├── validators/         # Zod schemas
│   │   │   └── utils/              # Utilities (date, format, etc.)
│   │   ├── __tests__/              # Vitest test files
│   │   │   ├── components/
│   │   │   └── lib/
│   │   ├── public/                 # Static assets
│   │   ├── package.json            # Next.js dependencies
│   │   ├── next.config.ts          # Next.js configuration
│   │   ├── tsconfig.json           # TypeScript config
│   │   ├── tailwind.config.ts      # TailwindCSS config
│   │   ├── vitest.config.ts        # Test configuration
│   │   ├── Dockerfile              # Multi-stage Docker build
│   │   └── .env.example            # Client env template
│   │
│   └── doctor-app/                 # Next.js 14 doctor portal
│       ├── app/
│       │   ├── (dashboard)/        # Dashboard layout group
│       │   │   ├── page.tsx        # Dashboard overview
│       │   │   ├── patients/       # Patient management (CRUD)
│       │   │   ├── schedule/       # Doctor schedule pages
│       │   │   ├── appointments/   # Appointment management
│       │   │   ├── medical-records/# Patient records access
│       │   │   └── prescriptions/  # Prescription issuing
│       │   └── api/
│       │       ├── auth/[...nextauth]/route.ts
│       │       ├── auth/token/route.ts
│       │       └── proxy/[...path]/route.ts
│       ├── components/             # React components
│       │   ├── dashboard/
│       │   ├── shared/
│       │   └── ui/ (shadcn/ui)
│       ├── lib/
│       │   ├── auth-config.ts      # NextAuth + Keycloak
│       │   ├── auth-session.ts     # Token management
│       │   ├── hooks/              # Custom React hooks
│       │   ├── validators/         # Zod schemas
│       │   └── utils/              # Utilities
│       ├── __tests__/              # Vitest test files (23 tests passing)
│       │   ├── components/
│       │   └── lib/
│       ├── public/                 # Static assets
│       ├── package.json            # Next.js dependencies
│       ├── next.config.ts          # Next.js configuration
│       ├── tsconfig.json           # TypeScript config
│       ├── tailwind.config.ts      # TailwindCSS config
│       ├── vitest.config.ts        # Test configuration
│       ├── Dockerfile              # Multi-stage Docker build
│       └── .env.example            # Client env template
│
├── gateway/
│   └── HospitalGateway/
│       ├── appsettings.json        # YARP routes, rate limits
│       ├── Program.cs              # Gateway setup, middleware
│       ├── Middleware/
│       │   ├── JwtAuthMiddleware.cs
│       │   └── RateLimitMiddleware.cs
│       └── HospitalGateway.csproj
│
├── services/
│   ├── PatientService/             # .NET 8, PostgreSQL — Full CRUD
│   │   ├── Controllers/
│   │   │   └── PatientsController.cs
│   │   ├── Domain/
│   │   │   ├── Patient.cs          # Aggregate root
│   │   │   └── PatientEvents.cs    # Domain events
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── CreatePatientCommand.cs
│   │   │   │   ├── UpdatePatientCommand.cs (NEW)
│   │   │   │   └── DeletePatientCommand.cs (NEW)
│   │   │   ├── CommandHandlers/
│   │   │   │   ├── CreatePatientHandler.cs
│   │   │   │   ├── UpdatePatientHandler.cs (NEW)
│   │   │   │   └── DeletePatientHandler.cs (NEW)
│   │   │   ├── Validators/
│   │   │   │   ├── CreatePatientValidator.cs
│   │   │   │   └── UpdatePatientValidator.cs (NEW)
│   │   │   └── Queries/
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── PatientsDbContext.cs
│   │   │   │   └── PatientsRepository.cs (with UpdateAsync)
│   │   │   └── MessageBus/
│   │   └── PatientService.csproj
│   │
│   ├── AppointmentService/         # .NET 8, PostgreSQL
│   │   ├── Controllers/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Infrastructure/
│   │   └── AppointmentService.csproj
│   │
│   ├── MedicalRecordService/       # Node.js, MongoDB
│   │   ├── src/
│   │   │   ├── controllers/
│   │   │   │   └── record-controller.js
│   │   │   ├── models/
│   │   │   │   └── Record.js       # Mongoose schema
│   │   │   ├── services/
│   │   │   │   └── record-service.js
│   │   │   ├── routes/
│   │   │   ├── middleware/
│   │   │   └── app.js              # Express app
│   │   ├── package.json
│   │   └── Dockerfile
│   │
│   ├── PharmacyService/            # Node.js, SQL Server
│   │   ├── src/
│   │   │   ├── controllers/
│   │   │   ├── models/
│   │   │   ├── services/
│   │   │   ├── routes/
│   │   │   └── app.js
│   │   ├── package.json
│   │   └── Dockerfile
│   │
│   ├── NotificationService/        # Node.js, Redis
│   │   ├── src/
│   │   │   ├── controllers/
│   │   │   ├── services/
│   │   │   │   ├── email-service.js
│   │   │   │   └── sms-service.js
│   │   │   ├── queues/
│   │   │   │   └── notification-queue.js
│   │   │   └── app.js
│   │   ├── package.json
│   │   └── Dockerfile
│   │
│   └── SearchService/              # Node.js, Elasticsearch
│       ├── src/
│       │   ├── controllers/
│       │   ├── services/
│       │   ├── indexer/
│       │   │   └── elasticsearch-indexer.js
│       │   └── app.js
│       ├── package.json
│       └── Dockerfile
│
├── shared/
│   ├── HospitalShared/             # .NET Shared Library
│   │   ├── DTOs/
│   │   │   ├── PatientDto.cs
│   │   │   ├── AppointmentDto.cs
│   │   │   └── MedicalRecordDto.cs
│   │   ├── Events/
│   │   │   ├── PatientCreatedEvent.cs
│   │   │   ├── AppointmentScheduledEvent.cs
│   │   │   └── MedicalRecordCreatedEvent.cs
│   │   ├── Constants/
│   │   └── HospitalShared.csproj
│   │
│   └── hospital-shared-js/         # Node.js Shared Library
│       ├── types/
│       │   ├── patient.types.js
│       │   ├── appointment.types.js
│       │   └── event.types.js
│       ├── utils/
│       │   ├── logger.js
│       │   ├── error-handler.js
│       │   └── validators.js
│       ├── constants/
│       │   └── event-types.js
│       └── package.json
│
├── infra/
│   ├── keycloak/                   # Keycloak config
│   │   └── realm-export.json       # Pre-configured realm
│   │
│   ├── prometheus/
│   │   └── prometheus.yml          # Scrape targets, retention
│   │
│   ├── grafana/
│   │   └── dashboards/
│   │       └── hospital-overview.json
│   │
│   ├── seq/
│   │   └── docker-compose.partial.yml
│   │
│   └── scripts/
│       ├── init-databases.sh       # Create databases, schemas
│       └── seed-data.sh            # Sample data for dev
│
├── docs/                           # This folder
│   ├── project-overview-pdr.md     # Project goals, PDR, timeline
│   ├── codebase-summary.md         # This file
│   ├── code-standards.md           # Coding conventions
│   ├── system-architecture.md      # System design, flows
│   ├── project-roadmap.md          # Phases, milestones
│   └── API-ENDPOINTS.md            # All endpoints (auto-generated)
│
├── tests/                          # Smoke & integration tests
│   ├── smoke/
│   │   └── smoke-test.sh           # Health endpoint verification (12 services)
│   ├── integration/
│   └── load/
│
└── README.md                       # Quick start, setup guide
```

---

## Service Inventory

### Client Applications

| Service | Port | Purpose | Tech Stack | Build |
|---|---|---|---|---|
| **Patient Portal** | 3100 | Patient-facing web app | Next.js 14 + TypeScript + shadcn/ui | npm build ✓ |
| **Doctor Portal** | 3200 | Doctor-facing portal + patient management | Next.js 14 + TypeScript + shadcn/ui | npm build ✓ |

### .NET 8 Services

| Service | Port | Database | Purpose | Key Entities |
|---|---|---|---|---|
| **PatientService** | 5001 | PostgreSQL | Patient CRUD (Create, Read, Update, Soft-Delete), profiles | Patient, Contact, Demographics |
| **AppointmentService** | 5002 | PostgreSQL | Scheduling, availability | Appointment, TimeSlot, Provider |
| **HospitalGateway** | 8000 | None | API gateway, routing | Routes, Policies |

### Node.js Services

| Service | Port | Database | Tech Stack | Build |
|---|---|---|---|---|
| **MedicalRecordService** | 5003 | MongoDB | Node.js + TypeScript + Mongoose | tsc ✓ |
| **PharmacyService** | 5004 | SQL Server | Node.js + TypeScript + Sequelize/tedious | tsc ✓ |
| **NotificationService** | 5005 | Redis | Node.js + TypeScript + ioredis + nodemailer | tsc ✓ |
| **SearchService** | 5006 | Elasticsearch | Node.js + TypeScript + @elastic/elasticsearch v8 | tsc ✓ |

---

## Patient Portal (Next.js 14) — Key Implementation Details

### Authentication (NextAuth v5 + Keycloak)

**File:** `lib/auth-config.ts`

Features:
- OIDC provider: Keycloak at `/realms/hospital`
- Server-side JWT validation with refresh support
- **Token Refresh Mutex Pattern:** Prevents concurrent refresh race conditions
  - Shared mutex via server-side state
  - Serializes refresh token requests
  - Maintains single source of truth for tokens
- ID token stored in server-side JWT only (never in session object exposed to client)
- Logout via Keycloak end_session endpoint

```typescript
// Example usage in components
const session = await auth();
const accessToken = await getAccessToken(); // Server-only, refreshes automatically
```

### API Proxy with Allowlist

**File:** `app/api/proxy/[...path]/route.ts`

Audit Fix F2: Explicit path allowlist prevents IDOR attacks.

```typescript
const ALLOWED_PATHS = [
  /^appointments(\/[^/]+)?(\/cancel)?$/,
  /^medical-records(\/[^/]+)?$/,
  /^prescriptions(\/[^/]+)?$/,
  /^patients\/[^/]+$/,
  /^providers\/?$/,
];

// Returns 403 for disallowed paths
// Forwards Authorization header with server-side access token
// Handles 204 No Content and non-JSON responses
```

### Directory Structure

```
lib/
├── auth-config.ts           # NextAuth + Keycloak setup
├── auth-session.ts          # getAccessToken() function
├── hooks/
│   ├── use-appointments.ts
│   ├── use-medical-records.ts
│   ├── use-prescriptions.ts
│   └── ... other custom hooks
├── validators/
│   ├── appointment-schema.ts # Zod validation
│   ├── patient-schema.ts
│   └── prescription-schema.ts
└── utils/
    ├── date-utils.ts
    ├── format-utils.ts
    └── ... other utilities

components/
├── dashboard/
│   ├── stats-cards.tsx       # Overview cards
│   └── ... other dashboard components
├── shared/
│   ├── status-badge.tsx      # Reusable components
│   └── ... other shared components
└── ui/
    └── shadcn/ui components (Dialog, Tabs, Select, etc.)

__tests__/
├── components/
│   ├── dashboard/stats-cards.test.tsx
│   └── shared/status-badge.test.tsx
└── lib/
    ├── utils/date-utils.test.ts
    ├── utils/format-utils.test.ts
    └── validators/
        ├── appointment-schema.test.ts
        └── patient-schema.test.ts
```

### Testing

**Framework:** Vitest + React Testing Library + JSDOM

**Test Coverage:** 40 unit tests
- Component rendering and interactions
- Utility functions (date parsing, formatting)
- Form validation schemas
- Hook behavior (if applicable)

**Run Tests:**
```bash
npm run test              # Single run
npm run test:watch       # Watch mode
npm run test:coverage    # Coverage report
```

---

## Shared Libraries

### HospitalShared (.NET)
**Purpose:** DTOs, domain events, constants shared across .NET services.

**Key Contents:**
- `PatientDto`, `AppointmentDto`, `MedicalRecordDto`
- `PatientCreatedEvent`, `AppointmentScheduledEvent`
- `EventType` enum, `HttpClientFactory` extensions
- `IMessagePublisher` interface for RabbitMQ abstraction

**Usage:**
```csharp
// PatientService references HospitalShared
using HospitalShared.DTOs;
using HospitalShared.Events;
```

### hospital-shared-js (Node.js)
**Purpose:** TypeScript types, validation utils, event constants shared across Node.js services.

**Key Contents:**
- `types/patient.types.js`, `types/appointment.types.js`
- `utils/logger.js` (Winston configuration), `utils/validators.js`
- `constants/event-types.js` (RabbitMQ event names)
- `utils/error-handler.js` (standardized error responses)

**Usage:**
```javascript
// MedicalRecordService
const { PatientType } = require('@hospital/shared/types');
const { logger } = require('@hospital/shared/utils');
```

---

## Communication Patterns

### API Gateway Flow
```
Client HTTP Request
    ↓
HospitalGateway (port 8000)
    ├→ Rate Limit Check
    ├→ JWT Validation (Keycloak)
    ├→ Route Rule Match
    └→ Forward to Service (port 5001, 3001, etc.)
```

### Async Event Flow
```
Service A publishes event (via RabbitMQ/MassTransit)
    ↓
Event Broker (RabbitMQ)
    ↓
Service B subscribes (via amqplib or MassTransit)
    └→ Process event asynchronously
```

**Example:** Appointment Created
```
AppointmentService publishes "AppointmentCreatedEvent"
    ↓
RabbitMQ broker routes to subscribers:
    ├→ NotificationService (sends SMS reminder)
    ├→ MedicalRecordService (creates empty record)
    └→ SearchService (indexes appointment data)
```

---

## Data Models Overview

### Patient Aggregate (Patient Service)
```
Patient
├── Id (Guid)
├── Email, Phone
├── FirstName, LastName
├── DateOfBirth
├── MedicalHistory (reference to MedicalRecordService)
├── Appointments (list of Appointment Ids)
└── CreatedAt, UpdatedAt
```

### Appointment Aggregate (Appointment Service)
```
Appointment
├── Id (Guid)
├── PatientId (foreign key to Patient Service)
├── ProviderId (doctor/staff)
├── ScheduledTime
├── Status (Scheduled, In Progress, Completed, Cancelled)
├── Notes
└── CreatedAt, UpdatedAt
```

### Medical Record (Medical Record Service)
```
Record
├── _id (MongoDB ObjectId)
├── patientId (reference to Patient Service)
├── appointmentId
├── findings, diagnosis, treatment
├── labResults (array)
├── documents (file metadata)
└── createdAt, updatedAt
```

### Drug/Prescription (Pharmacy Service)
```
Drug
├── Id, Code, Name
├── Dosage, Unit
├── Price, Expiration
├── InventoryLevel
└── Supplier

Prescription
├── Id
├── PatientId
├── DrugId
├── Quantity, Instructions
├── IssuedAt, ValidUntil
└── Status
```

---

## Environment Configuration

### .env Variables (Development)

```bash
# Gateway
GATEWAY_PORT=8000
JWT_ISSUER=http://keycloak:8080/realms/hospital
JWT_AUDIENCE=hospital-gateway

# .NET Services
PATIENT_SERVICE_URL=http://patient-service:5001
APPOINTMENT_SERVICE_URL=http://appointment-service:5002
POSTGRES_USER=hospital
POSTGRES_PASSWORD=dev_password
POSTGRES_DB=hospital_db

# Node.js Services
MEDICAL_RECORD_SERVICE_URL=http://medical-record-service:3001
PHARMACY_SERVICE_URL=http://pharmacy-service:3002
NOTIFICATION_SERVICE_URL=http://notification-service:3003
SEARCH_SERVICE_URL=http://search-service:3004
MONGODB_URI=mongodb://mongo:27017/hospital
MSSQL_SERVER=sqlserver
MSSQL_USER=sa
MSSQL_PASSWORD=YourStrong!Passw0rd

# Message Broker
RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_PORT=5672

# Cache & Search
REDIS_HOST=redis
REDIS_PORT=6379
ELASTICSEARCH_URI=http://elasticsearch:9200

# Keycloak
KEYCLOAK_URL=http://keycloak:8080
KEYCLOAK_REALM=hospital
KEYCLOAK_CLIENT_ID=hospital-gateway
KEYCLOAK_CLIENT_SECRET=xxx

# Monitoring
SEQ_URL=http://seq:5341
```

---

## Dependencies & Versions

### .NET 8 Packages
- `Entity Framework Core 8.0`
- `MassTransit 8.1` (async messaging)
- `YARP 2.1` (API gateway)
- `Serilog` (structured logging)
- `AutoMapper` (DTO mapping)

### Node.js Packages
- `express 4.18`
- `mongoose 7.x` (MongoDB ODM)
- `amqplib 0.10` (RabbitMQ client)
- `winston` (logging)
- `jest` (testing)
- `axios` (HTTP client)

### Infrastructure
- **Docker** 24.x
- **PostgreSQL** 15.x
- **MongoDB** 6.x
- **SQL Server** 2022
- **RabbitMQ** 3.12
- **Redis** 7.x
- **Elasticsearch** 8.x
- **Keycloak** 21.x
- **Prometheus** 2.45
- **Grafana** 10.x
- **Seq** 2023.4

---

## Building & Running Locally

### Prerequisites
- Docker & Docker Compose 24.x
- .NET 8 SDK
- Node.js 18+ with npm
- Git

### Quick Start
```bash
# Clone repo
git clone <repo>
cd hospital-microservices

# Copy environment template
cp .env.example .env

# Build and start all containers
docker-compose up -d

# Verify services are running
docker-compose ps

# View logs (optional)
docker-compose logs -f
```

### Access Points (Development)
- **Patient Portal:** http://localhost:3100 (Next.js 14 + TypeScript)
- **Doctor Portal:** http://localhost:3200 (Next.js 14 + TypeScript)
- **Gateway:** http://localhost:8000 (YARP, .NET 8)
- **Patient Service:** http://localhost:5001 (.NET 8 + PostgreSQL)
- **Appointment Service:** http://localhost:5002 (.NET 8 + PostgreSQL)
- **Medical Record Service:** http://localhost:5003 (Node.js + MongoDB)
- **Pharmacy Service:** http://localhost:5004 (Node.js + SQL Server)
- **Notification Service:** http://localhost:5005 (Node.js + Redis)
- **Search Service:** http://localhost:5006 (Node.js + Elasticsearch)
- **Keycloak:** http://localhost:8080 (OAuth2/OIDC)
- **Prometheus:** http://localhost:9090 (Metrics)
- **Grafana:** http://localhost:3000 (Dashboards)
- **Seq:** http://localhost:5341 (Logging)

---

## Code Organization Principles

1. **Service Isolation** — Each service owns its domain, database, and API contract
2. **Async First** — Use messaging for cross-service communication (eventual consistency)
3. **Shared Contracts** — DTOs and events defined in shared libraries, used consistently
4. **Clean Architecture** — Controllers → Application → Domain → Infrastructure layers
5. **Health Checks** — Every service exposes `/health` endpoint
6. **Structured Logging** — All logs include correlation IDs and service context
7. **No Direct Database Access Between Services** — Always use APIs or events

---

## Testing Strategy

| Test Type | Scope | Tool | Example |
|---|---|---|---|
| **Unit Tests** | Single function/class | xUnit (.NET), Jest (Node.js) | PatientService.CreatePatient() |
| **Integration Tests** | Service + Database | xUnit/Jest + Docker | PatientService → PostgreSQL flow |
| **API Tests** | HTTP endpoints | Postman, REST Assured | GET /patients/{id} returns 200 |
| **Event Tests** | Message flow | MassTransit test harness | AppointmentCreated → Notification sent |
| **Load Tests** | Throughput, latency | k6, JMeter | 1000 concurrent /patients requests |

---

## Build Verification

All services verified buildable:
- **Patient Portal:** `npm run build` ✓ — 40 tests passing
- **Doctor Portal:** `npm run build` ✓ — 23 tests passing
- **HospitalGateway:** `dotnet build` ✓
- **PatientService:** `dotnet build` ✓
- **AppointmentService:** `dotnet build` ✓
- **MedicalRecordService:** `tsc` ✓
- **PharmacyService:** `tsc` ✓
- **NotificationService:** `tsc` ✓
- **SearchService:** `tsc` ✓
- **HospitalShared (.NET):** `dotnet build` ✓
- **hospital-shared-js (Node.js):** `tsc` ✓

Testing:
- **Patient Portal Unit Tests:** `npm run test` — 40 tests passing in Vitest
- **Doctor Portal Unit Tests:** `npm run test` — 23 tests passing in Vitest
- **Smoke Tests:** `tests/smoke/smoke-test.sh` — Validates all service health endpoints

Docker Compose: 18 services (10 infra + 8 app) with healthchecks.

## Known Limitations & TODOs

- [ ] Database migrations not yet generated — run EF Core migrations for .NET services
- [ ] Keycloak realm config is template — requires customization per environment
- [ ] Elasticsearch indexing strategy needs finalization (real-time vs batch)
- [ ] Distributed tracing (OpenTelemetry) scope to be defined in Phase 6
- [ ] Multi-tenancy not in initial scope (single hospital instance)
- [ ] API versioning strategy (v1, v2) to be finalized before Phase 2

---

## Document Versions

| File | Last Updated | Version |
|---|---|---|
| codebase-summary.md | 2026-03-20 | 1.2 |
| Related: project-overview-pdr.md | 2026-03-18 | 1.0 |
| Related: code-standards.md | 2026-03-18 | 1.0 |
| Related: system-architecture.md | 2026-03-20 | 1.3 |
| Related: project-roadmap.md | 2026-03-19 | 1.1 |

