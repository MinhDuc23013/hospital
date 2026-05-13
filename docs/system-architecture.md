# Hospital HRM Microservices — System Architecture

**Version:** 1.3
**Last Updated:** 2026-03-20
**Status:** Patient Portal + Doctor Portal with Patient Management Complete — 8 microservices + gateway + 2 Next.js portals with full CRUD patient features

---

## Architecture Overview

The Hospital HRM system follows a **microservices architecture** with decoupled services communicating via HTTP REST APIs (through a central gateway) and asynchronous messaging (RabbitMQ). Each service owns its domain logic, database, and deployment lifecycle.

```
┌──────────────────────────────────────────────────────────────────────┐
│                         Client Layer                                  │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ Patient Portal (Next.js 14, Port 3100)                         │ │
│  │ ├─ NextAuth v5 + Keycloak OIDC                                │ │
│  │ ├─ Token Refresh Mutex (prevents race conditions)              │ │
│  │ ├─ API Proxy with Allowlist                                   │ │
│  │ └─ shadcn/ui + TailwindCSS (responsive UI)                    │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────┬────────────────────────────────────────────────┘
                       │ HTTPS (via API Proxy)
┌──────────────────────▼────────────────────────────────────────────────┐
│            API Gateway (YARP, .NET 8, Port 8000)                      │
│  ├─ JWT Token Validation (Keycloak)                                   │
│  ├─ Rate Limiting                                                      │
│  ├─ Request Routing                                                    │
│  └─ Health Check                                                       │
└───┬────────────┬────────────┬───────────┬────────────┬────────────────┘
    │            │            │           │            │
    │ REST/HTTP  │ REST/HTTP  │ REST/HTTP │ REST/HTTP  │ REST/HTTP
    ▼            ▼            ▼           ▼            ▼
┌────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│Patient │ │Appointment│Medical   │Pharmacy  │Notification│
│Service │ │Service   │Record    │Service   │Service     │
│.NET 8  │ │.NET 8    │Service   │Node.js   │Node.js     │
│PG      │ │PG        │Node.js   │SQL Srv   │Redis       │
│        │ │          │MongoDB   │          │            │
└────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘
    │          │            │            │            │
    └──────────┴────────────┴────────────┴────────────┘
             │
             │ RabbitMQ (Async Events)
             ▼
    ┌─────────────────────┐
    │   Message Broker    │
    │   (RabbitMQ 3.12)   │
    │  ├─ Event Topics    │
    │  ├─ Dead Letter Q   │
    │  └─ Retry Policy    │
    └─────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                          │
├─────────────────────────────────────────────────────────────────┤
│ ┌──────────────┐ ┌──────────────┐ ┌──────────┐ ┌──────────┐    │
│ │PostgreSQL 15 │ │Elasticsearch │ │ Redis 7  │ │ Keycloak │    │
│ │ (Patient,    │ │ (Patient &   │ │(Cache &  │ │ (OAuth2/ │    │
│ │ Appointment) │ │  Drug Index) │ │   Pub/Sub)│ │  OIDC)   │    │
│ └──────────────┘ └──────────────┘ └──────────┘ └──────────┘    │
│                                                                  │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────┐ ┌──────────┐    │
│ │ MongoDB 6    │ │ SQL Server   │ │Prometheus│ │ Grafana  │    │
│ │(Medical Rec.)│ │ (Pharmacy)   │ │(Metrics) │ │(Dashboard)   │
│ └──────────────┘ └──────────────┘ └──────────┘ └──────────┘    │
│                                                                  │
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ Seq (Centralized Logging) + OpenTelemetry (Tracing)      │   │
│ └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Service Inventory

### 0. Patient Portal (Client)

**Technology:** Next.js 14 App Router, TypeScript, Node.js 18
**Port:** 3100
**Deployment:** Docker service: `patient-client`

**Responsibilities:**
- Patient-facing web application for managing medical records and appointments
- Server-side authentication via NextAuth v5
- Client-to-backend communication through secure API proxy

**Key Features:**
- **Authentication:** NextAuth v5 + Keycloak OIDC provider
  - Server-side JWT validation with refresh token support
  - Token refresh uses mutex pattern to prevent concurrent refresh race conditions
  - ID token stored server-side only (never exposed to client)
  - Secure logout via Keycloak end_session endpoint

- **API Proxy:** `/api/proxy/[...path]` with explicit path allowlist
  - Allowed paths: `appointments`, `medical-records`, `prescriptions`, `patients/{id}`, `providers`
  - Prevents IDOR attacks through explicit whitelist (audit fix F2)
  - Forwards Authorization header with server-side access token
  - Returns 403 for disallowed paths

- **UI Components:** shadcn/ui components over TailwindCSS
  - Responsive dashboard with stats cards
  - Appointment booking and management
  - Medical records viewer
  - Prescriptions list
  - Profile management
  - Provider directory

**Pages & Routes:**
```
/(dashboard)
├── page.tsx                    # Dashboard overview
├── patients/
│   ├── page.tsx               # Patients list
│   ├── new/page.tsx           # Create new patient
│   └── [id]/
│       ├── page.tsx           # Patient profile view
│       └── edit/page.tsx      # Edit patient form
├── appointments/
│   ├── page.tsx               # Appointments list
│   └── [id]/page.tsx          # Appointment details
├── medical-records/
│   ├── page.tsx               # Records list
│   └── [id]/page.tsx          # Record details
├── prescriptions/
│   ├── page.tsx               # Prescriptions list
│   └── [id]/page.tsx          # Prescription details
└── schedule/
    └── page.tsx               # Doctor schedule
```

**API Routes:**
```
/api/auth/[...nextauth]        # NextAuth v5 dynamic route
/api/auth/token                # Returns idToken for logout
/api/proxy/[...path]           # Proxies to gateway with allowlist
```

**Key Libraries:**
- `next-auth@5.0.0-beta` — OIDC authentication
- `@tanstack/react-query@5.x` — Data fetching & caching
- `react-hook-form` + `zod` — Form validation
- `@radix-ui/*` — Accessible components
- `tailwindcss` + `tailwind-merge` — Styling
- `jwt-decode` — Client-side token inspection
- `date-fns` — Date utilities

**Testing:**
- Framework: Vitest + React Testing Library
- 40 unit tests covering components, utilities, and validators
- Test files in `__tests__/` directory:
  - Component tests (stats-cards, status-badge)
  - Utility tests (date-utils, format-utils)
  - Schema validation tests (appointment, patient schemas)

**Security Considerations:**
- No API credentials stored in client environment (all via server)
- NEXT_PUBLIC_* prefix only for non-sensitive config
- GATEWAY_API_URL is server-only (prevents direct client→gateway calls)
- Access tokens refreshed server-side via mutex to prevent race conditions
- ID token never exposed to JavaScript (audit fix F11)

**Docker Deployment:**
- Multi-stage build (builder + runner)
- Node.js 18-alpine base image
- Standalone output from Next.js (optimized ~200MB)
- Non-root user (nextjs:nodejs) for security
- Healthcheck via localhost:3100 (automatic by docker-compose)

---

### 0a. Doctor Portal (Client)

**Technology:** Next.js 14 App Router, TypeScript, Node.js 18
**Port:** 3200
**Deployment:** Docker service: `doctor-client`

**Responsibilities:**
- Doctor-facing web application for patient management, scheduling, and medical records
- Server-side authentication via NextAuth v5
- Client-to-backend communication through secure API proxy

**Key Features:**
- **Authentication:** NextAuth v5 + Keycloak OIDC provider (same as Patient Portal)
  - Server-side JWT validation with refresh token support
  - Token refresh uses mutex pattern to prevent concurrent refresh race conditions
  - Secure logout via Keycloak end_session endpoint

- **Patient Management (NEW):** Full CRUD operations for patients
  - Create new patient (firstName, lastName, phoneNumber, email, dateOfBirth)
  - View patient list with pagination
  - Edit patient details (firstName, lastName, phoneNumber)
  - Deactivate patients (soft-delete via patient.Deactivate())
  - View patient profile with Edit + Deactivate buttons (active patients only)

- **UI Components:** shadcn/ui components over TailwindCSS
  - Responsive dashboard with stats cards
  - Patient management forms (PatientCreateForm, PatientEditForm)
  - Patient delete dialog (PatientDeleteDialog)
  - Schedule management
  - Appointment management
  - Medical records viewer
  - Prescription management

**Pages & Routes:**
```
/(dashboard)
├── page.tsx                    # Dashboard overview
├── patients/
│   ├── page.tsx               # Patients list with "New Patient" button
│   ├── new/page.tsx           # Create new patient form
│   └── [id]/
│       ├── page.tsx           # Patient profile view (Edit + Deactivate buttons)
│       └── edit/page.tsx      # Edit patient form
├── appointments/
│   ├── page.tsx               # Appointments list
│   └── [id]/page.tsx          # Appointment details
├── medical-records/
│   ├── page.tsx               # Records list
│   └── [id]/page.tsx          # Record details
├── prescriptions/
│   ├── page.tsx               # Prescriptions list
│   └── [id]/page.tsx          # Prescription details
└── schedule/
    └── page.tsx               # Doctor schedule
```

**API Routes:**
```
/api/auth/[...nextauth]        # NextAuth v5 dynamic route
/api/auth/token                # Returns idToken for logout
/api/proxy/[...path]           # Proxies to gateway with allowlist
```

**Custom Hooks (TanStack Query v5):**
- `useCreatePatient()` — POST /api/proxy/patients
- `useUpdatePatient()` — PUT /api/proxy/patients/{id}
- `useDeletePatient()` — DELETE /api/proxy/patients/{id}

**Validators (Zod):**
- `createPatientSchema` — Validates new patient form
- `updatePatientSchema` — Validates patient update form

**Key Libraries:**
- `next-auth@5.0.0-beta` — OIDC authentication
- `@tanstack/react-query@5.x` — Data fetching & caching
- `react-hook-form` + `zod` — Form validation
- `@radix-ui/*` — Accessible components
- `tailwindcss` + `tailwind-merge` — Styling
- `jwt-decode` — Client-side token inspection

**Testing:**
- Framework: Vitest + React Testing Library
- 23 unit tests covering components, utilities, and validators

**Security Considerations:**
- No API credentials stored in client environment (all via server)
- NEXT_PUBLIC_* prefix only for non-sensitive config
- GATEWAY_API_URL is server-only (prevents direct client→gateway calls)
- Access tokens refreshed server-side via mutex to prevent race conditions

**Docker Deployment:**
- Multi-stage build (builder + runner)
- Node.js 18-alpine base image
- Standalone output from Next.js (optimized ~200MB)
- Non-root user (nextjs:nodejs) for security
- Healthcheck via localhost:3200 (automatic by docker-compose)

---

### 1. API Gateway (HospitalGateway)

**Technology:** .NET 8, YARP
**Port:** 8000
**Build Status:** ✓ dotnet build verified
**Responsibility:** Route requests, authenticate, rate limit, health check

**Key Features:**
- JWT validation against Keycloak
- Per-route rate limiting (e.g., 100 requests/min per user)
- Service discovery and load balancing
- Request/response logging with correlation IDs
- Circuit breaker pattern for downstream services

**Routes:**
```
POST   /api/patients              → PatientService
GET    /api/patients              → PatientService (list)
GET    /api/patients/{id}         → PatientService
PUT    /api/patients/{id}         → PatientService
DELETE /api/patients/{id}         → PatientService
POST   /api/appointments          → AppointmentService
GET    /api/appointments          → AppointmentService (list)
GET    /api/appointments/{id}     → AppointmentService
GET    /api/medical-records/{id}  → MedicalRecordService
POST   /api/prescriptions         → PharmacyService
POST   /api/notifications/send    → NotificationService
GET    /api/search                → SearchService
```

**Health:** GET `/health` (returns service status)

---

### 2. Patient Service

**Technology:** .NET 8 + EF Core + Npgsql, PostgreSQL
**Port:** 5001
**Database:** PostgreSQL `hospital_db.patients` table
**Build Status:** ✓ dotnet build verified

**Responsibilities:**
- Patient profile CRUD
- Patient demographics and contact info
- Patient history and relationships

**Key Entities:**
```
Patient
├── Id (Guid, PK)
├── Email (string, unique)
├── FirstName, LastName
├── DateOfBirth
├── PhoneNumber
├── Address
├── CreatedAt, UpdatedAt
└── IsActive
```

**API Endpoints:**
```
POST   /api/patients              # Create patient
GET    /api/patients              # List patients (paginated)
GET    /api/patients/{id}         # Get patient details
PUT    /api/patients/{id}         # Update patient (firstName, lastName, phoneNumber)
DELETE /api/patients/{id}         # Soft delete patient (calls patient.Deactivate())
GET    /api/patients/{id}/appointments  # Get patient's appointments
```

**Handlers:**
- UpdatePatientHandler — Updates patient fields (firstName, lastName, phoneNumber)
- DeletePatientHandler — Soft-deletes patient via Deactivate()

**Validators:**
- UpdatePatientValidator — Validates update payload

**Events Published:**
- `PatientCreatedEvent` → Notification Service (send welcome email)
- `PatientUpdatedEvent` → Search Service (re-index)

**Dependencies:**
- PostgreSQL (data)
- RabbitMQ (publish events)
- Redis (cache patient profiles)
- IPatientRepository with UpdateAsync method

---

### 3. Appointment Service

**Technology:** .NET 8 + EF Core + Npgsql, PostgreSQL
**Port:** 5002
**Database:** PostgreSQL `hospital_db.appointments` table
**Build Status:** ✓ dotnet build verified

**Responsibilities:**
- Appointment scheduling and management
- Availability checks
- Appointment status tracking

**Key Entities:**
```
Appointment
├── Id (Guid, PK)
├── PatientId (Guid, FK to Patient Service)
├── ProviderId (string - doctor ID)
├── ScheduledTime (DateTime)
├── Duration (TimeSpan)
├── Status (Scheduled, In Progress, Completed, Cancelled)
├── Notes
└── CreatedAt, UpdatedAt

TimeSlot (for availability)
├── Id (Guid, PK)
├── ProviderId
├── SlotDateTime
└── IsAvailable
```

**API Endpoints:**
```
POST   /api/appointments           # Schedule appointment
GET    /api/appointments           # List appointments (filter by patient/provider)
GET    /api/appointments/{id}      # Get appointment details
PUT    /api/appointments/{id}      # Update appointment
DELETE /api/appointments/{id}      # Cancel appointment
GET    /api/providers/{providerId}/availability  # Check availability
```

**Events Published:**
- `AppointmentScheduledEvent` → Notification Service (send SMS reminder)
- `AppointmentScheduledEvent` → Medical Record Service (create empty record)
- `AppointmentCancelledEvent` → Notification Service, Search Service

**Dependencies:**
- PostgreSQL (data)
- RabbitMQ (publish events)
- HTTP call to Patient Service (validate patient exists)

---

### 4. Medical Record Service

**Technology:** Node.js + TypeScript + Express + Mongoose, MongoDB
**Port:** 5003
**Database:** MongoDB `hospital_db.medical_records` collection
**Build Status:** ✓ tsc verified

**Responsibilities:**
- Store medical records, diagnoses, lab results
- Document management (PDF, images)
- Audit trail for compliance

**Key Entities:**
```
MedicalRecord (MongoDB document)
{
  _id: ObjectId,
  patientId: UUID,
  appointmentId: UUID,
  findings: string,
  diagnosis: [string],
  labResults: [{
    testName: string,
    result: string,
    normalRange: string,
    timestamp: Date
  }],
  documents: [{
    filename: string,
    s3Url: string,
    uploadedAt: Date
  }],
  createdBy: string (doctor ID),
  createdAt: Date,
  updatedAt: Date
}
```

**API Endpoints:**
```
GET    /api/medical-records/{patientId}     # Get records for patient
POST   /api/medical-records                 # Create record
PUT    /api/medical-records/{id}            # Update record
GET    /api/medical-records/{id}/documents  # List documents
POST   /api/medical-records/{id}/documents  # Upload document
```

**Event Handlers:**
- Subscribes to `AppointmentScheduledEvent` → Creates empty record
- Subscribes to `PrescriptionIssuedEvent` → Updates record with prescription

**Dependencies:**
- MongoDB (data)
- RabbitMQ (subscribe to events)
- S3 or local storage (document uploads)

---

### 5. Pharmacy Service

**Technology:** Node.js + TypeScript + Express + Sequelize/tedious, SQL Server
**Port:** 5004
**Database:** SQL Server `hospital_db.pharmacy` schema
**Build Status:** ✓ tsc verified

**Responsibilities:**
- Drug inventory management
- Prescription processing
- Stock tracking and alerts

**Key Entities:**
```
Drug
├── Id (int, PK)
├── Code (string, unique)
├── Name
├── Dosage, Unit
├── Price
├── ExpirationDate
├── CurrentStock
├── MinimumStock (alert threshold)
└── Supplier

Prescription
├── Id (Guid, PK)
├── PatientId (Guid)
├── DrugId (FK)
├── Quantity
├── Instructions
├── IssuedAt, ValidUntil
└── Status (Pending, Dispensed, Expired)
```

**API Endpoints:**
```
GET    /api/drugs                  # List drugs (with search)
GET    /api/drugs/{id}             # Get drug details
POST   /api/prescriptions          # Create prescription
GET    /api/prescriptions/{id}     # Get prescription
PUT    /api/prescriptions/{id}/dispense  # Mark as dispensed
GET    /api/inventory/low-stock    # Alert on low stock
```

**Event Publishers:**
- `PrescriptionIssuedEvent` → Medical Record Service
- `InventoryLowEvent` → Notification Service (alert pharmacy staff)

**Dependencies:**
- SQL Server (data)
- RabbitMQ (publish events)
- HTTP call to Medical Record Service (verify appointment)

---

### 6. Notification Service

**Technology:** Node.js + TypeScript + Express + ioredis + nodemailer, Redis
**Port:** 5005
**Database:** Redis (queue only, no persistent storage)
**Build Status:** ✓ tsc verified

**Responsibilities:**
- Send email/SMS/push notifications
- Manage notification templates
- Retry failed notifications

**Key Features:**
- Template system (appointment reminders, lab results, etc.)
- Multi-channel (Email via Nodemailer, SMS via Twilio)
- Retry with exponential backoff
- Rate limiting per user

**API Endpoints:**
```
POST   /api/notifications/send    # Send immediate notification
GET    /api/notifications/templates # List templates
POST   /api/notifications/templates # Create template
GET    /api/notifications/logs     # View notification history
```

**Event Handlers:**
- Subscribes to `AppointmentScheduledEvent` → Send SMS reminder
- Subscribes to `PatientCreatedEvent` → Send welcome email
- Subscribes to `LabResultReadyEvent` → Send email
- Subscribes to `InventoryLowEvent` → Alert pharmacy staff

**Dependencies:**
- RabbitMQ (subscribe to events)
- Redis (queue notifications)
- Email service (Nodemailer + SMTP)
- SMS service (Twilio API)
- Seq (logging)

---

### 7. Search Service

**Technology:** Node.js + TypeScript + Express + @elastic/elasticsearch v8, Elasticsearch
**Port:** 5006
**Database:** Elasticsearch `hospital-patients`, `hospital-drugs` indices
**Build Status:** ✓ tsc verified

**Responsibilities:**
- Full-text search for patients and drugs
- Real-time indexing via events
- Search analytics

**API Endpoints:**
```
GET    /api/search?q=john&type=patient  # Search patients
GET    /api/search?q=aspirin&type=drug  # Search drugs
GET    /api/search/analytics            # Search stats
```

**Event Handlers:**
- Subscribes to `PatientCreatedEvent`, `PatientUpdatedEvent` → Index/re-index patient
- Subscribes to `AppointmentScheduledEvent` → Update patient document

**Dependencies:**
- Elasticsearch (indexing and search)
- RabbitMQ (subscribe to events)
- Node.js `@elastic/elasticsearch` client

---

### 8. AI RCA Service

**Technology:** .NET 8 + Anthropic Claude API, Loki + Jaeger
**Port:** 5013
**Database:** Loki (log store), Redis (cache), Jaeger (traces)
**Build Status:** ✓ dotnet build verified
**Gateway Route:** `/api/ai-rca/*` → `http://ai-rca-service:5013/`

**Responsibilities:**
- On-demand root cause analysis for log events
- Query logs from Loki, redact PHI (PII), call Claude AI API
- Cache RCA results for frequently investigated patterns
- Integrate with Grafana data links for quick analysis

**Key Features:**
- **Data Link Integration:** Grafana panel button → AiRcaService → HTML result in browser
- **PII Redaction:** Regex-based redaction for Vietnamese patient data (phone, ID, email)
- **Jaeger Tracing:** Distributed trace context propagation via W3C Trace Context
- **Redis Caching:** Cache RCA results by log hash (30% target cache hit rate)
- **Structured Output:** Markdown result → HTML rendering (root cause + fix suggestion)

**API Endpoints:**
```
POST   /api/ai-rca/analyze          # Analyze logs via Grafana data link
GET    /api/ai-rca/health           # Health check endpoint
```

**Dependencies:**
- Loki (log aggregation)
- Anthropic Claude API (LLM provider)
- Redis (result caching)
- Jaeger (distributed tracing, optional)
- Grafana (data link configuration)

**Environment Variables:**
- `ANTHROPIC_API_KEY` — Claude API authentication (required)
- `LOKI_URL` — Loki query endpoint (default: http://loki:3100)
- `REDIS_URL` — Redis connection (default: redis://redis:6379)
- `JAEGER_ENABLED` — Enable tracing (default: true)

---

## Infrastructure & Orchestration

### Docker Compose
- **Total Services:** 18 (10 infrastructure + 8 application services)
  - Application services: patient-client, gateway, patient-service, appointment-service, medical-record-service, pharmacy-service, notification-service, search-service
- **Status:** All services have healthchecks configured
- **Configuration:** docker-compose.yml (primary) + docker-compose.override.yml (dev overrides)

### Shared Libraries

**HospitalShared (.NET 8)**
- **Build Status:** ✓ dotnet build verified
- **Contents:** DTOs, domain events, MassTransit 8.1.3 message bus abstraction, constants
- **Usage:** Referenced by PatientService, AppointmentService, HospitalGateway

**hospital-shared-js (Node.js TypeScript)**
- **Build Status:** ✓ tsc verified
- **Contents:** Type definitions, validation utilities, Winston logging, error handlers, event constants
- **Usage:** Imported as @hospital/shared by MedicalRecordService, PharmacyService, NotificationService, SearchService

### Testing Infrastructure
- **Smoke Tests:** `tests/smoke/smoke-test.sh` — Validates all service health endpoints
- **Patient Client Tests:** 40 unit tests in `__tests__/` covering components, utilities, and validators
- **Status:** Ready for validation after docker-compose up

---

## Data Flow Scenarios

### Scenario 1: Patient Registration & Welcome

```
1. Client POST /api/patients (name, email, DOB)
   ├─ Gateway validates JWT
   ├─ Routes to Patient Service (5001)

2. Patient Service
   ├─ Validates input (email format, etc.)
   ├─ Creates Patient in PostgreSQL
   ├─ Publishes PatientCreatedEvent to RabbitMQ
   └─ Returns PatientDto (201 Created)

3. RabbitMQ routes PatientCreatedEvent to:
   ├─ Notification Service
   │  └─ Sends welcome email (async, via Nodemailer)
   └─ Search Service
      └─ Indexes patient in Elasticsearch
```

**Response Time:** <500ms (patient created), <60s (email sent)

---

### Scenario 2: Appointment Booking

```
1. Client POST /api/appointments (patientId, providerId, dateTime)
   ├─ Gateway validates JWT
   ├─ Routes to Appointment Service (5002)

2. Appointment Service
   ├─ Validates appointment input
   ├─ Calls Patient Service HTTP GET /api/patients/{patientId} (verify patient)
   ├─ Checks available time slots in PostgreSQL
   ├─ Creates Appointment in PostgreSQL
   ├─ Publishes AppointmentScheduledEvent to RabbitMQ
   └─ Returns AppointmentDto (201 Created)

3. RabbitMQ routes AppointmentScheduledEvent to:
   ├─ Notification Service
   │  └─ Sends SMS reminder (async, via Twilio)
   ├─ Medical Record Service
   │  └─ Creates empty MedicalRecord in MongoDB
   └─ Search Service
      └─ Updates patient index with appointment info

4. Medical Record Service (on receipt of event)
   ├─ Stores MedicalRecord.appointmentId
   └─ Initializes empty record (findings, labs) for doctor to fill
```

**Response Time:** <1000ms (appointment booked), <60s (SMS sent + record created)

---

### Scenario 3: Prescription & Pharmacy

```
1. Doctor adds prescription during/after appointment
   ├─ Client POST /api/prescriptions (appointmentId, drugId, quantity)
   └─ Routes to Pharmacy Service (3002)

2. Pharmacy Service
   ├─ Validates prescription input
   ├─ Calls Medical Record Service HTTP GET (verify appointment exists)
   ├─ Checks drug availability in SQL Server
   ├─ Creates Prescription in SQL Server
   ├─ Publishes PrescriptionIssuedEvent to RabbitMQ
   └─ Returns PrescriptionDto (201 Created)

3. RabbitMQ routes PrescriptionIssuedEvent to:
   ├─ Medical Record Service
   │  └─ Appends prescription to patient's MedicalRecord
   ├─ Notification Service
   │  └─ Sends email: "Prescription ready for pickup"
   └─ Possibly Inventory Service (decrement stock)
```

**Response Time:** <800ms (prescription created), <60s (email sent)

---

### Scenario 4: Search for Patient

```
1. Client GET /api/search?q=john&type=patient
   ├─ Gateway validates JWT
   └─ Routes to Search Service (3004)

2. Search Service
   ├─ Queries Elasticsearch index "hospital-patients"
   ├─ Returns matching patients (name, ID, contact)
   └─ Returns SearchResultDto (200 OK)

Response: <200ms
```

---

## Communication Patterns

### Synchronous (HTTP REST)

| From | To | Purpose | Timeout |
|---|---|---|---|
| Gateway | Patient/Appointment Service | API requests | 30s |
| Appointment Service | Patient Service | Validate patient exists | 5s |
| Pharmacy Service | Medical Record Service | Verify appointment | 5s |
| Any Service | Search Service | Index/query operations | 10s |

**Resilience:**
- Circuit breaker: Open after 5 failures within 10s window
- Retry: Exponential backoff (1s, 2s, 4s)
- Fallback: Return cached data or 503 Service Unavailable

### Asynchronous (RabbitMQ)

**Message Flow:**
```
Service A publishes event to RabbitMQ Topic Exchange (fanout)
    ↓
RabbitMQ routing (topic-based)
    ↓
Service B Queue (durable) ← subscribes
Service C Queue (durable) ← subscribes
    ↓
Service B/C processes message
    ↓
(On error) Dead Letter Queue (DLQ)
```

**Delivery Guarantees:**
- At-least-once delivery (MassTransit/amqplib with acknowledgements)
- Dead Letter Queue for failed messages
- Configurable retry policy (max 3 retries, then DLQ)

**Events:**
| Event | Publisher | Subscribers | Latency Target |
|---|---|---|---|
| PatientCreatedEvent | Patient Service | Notification, Search | <5s |
| AppointmentScheduledEvent | Appointment Service | Notification, Medical Record, Search | <5s |
| PrescriptionIssuedEvent | Pharmacy Service | Medical Record, Notification | <5s |
| LabResultReadyEvent | Medical Record Service | Notification | <5s |
| InventoryLowEvent | Pharmacy Service | Notification (alert staff) | <10s |

---


---

> **Continued in:** [system-architecture-operations.md](./system-architecture-operations.md) — Caching, Security, Monitoring, Deployment, DR, Scaling
