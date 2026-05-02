# Hospital HRM Microservices

A scalable, modular **Hospital Resource Management (HRM) system** built with microservices architecture on .NET 8 and Node.js.

**Status:** Scaffold complete — all 7 services running (2026-03-18)

---

## Quick Overview

The Hospital HRM system enables hospitals to manage:
- **Patient Management** — Patient profiles, demographics, medical history
- **Appointments** — Scheduling, availability, appointment tracking
- **Medical Records** — Lab results, diagnoses, documents
- **Pharmacy** — Drug inventory, prescriptions, stock management
- **Notifications** — Email/SMS reminders, alerts
- **Search** — Full-text search for patients and drugs

### Architecture Highlights
- **API Gateway:** YARP (.NET 8) with JWT authentication and rate limiting
- **.NET 8 Services:** Patient Service, Appointment Service (PostgreSQL)
- **Node.js Services:** Medical Record (MongoDB), Pharmacy (SQL Server), Notification (Redis), Search (Elasticsearch)
- **Async Messaging:** RabbitMQ for event-driven communication
- **Infrastructure:** Docker Compose, Keycloak (IAM), Prometheus/Grafana (monitoring), Seq (logging)

---

## Getting Started

### Prerequisites
- **Docker & Docker Compose** 24.x
- **.NET 8 SDK** (for local .NET development)
- **Node.js** 18+ with npm (for local Node.js development)
- **Git**

### Development Setup (5 minutes)

1. **Clone and navigate to project**
   ```bash
   git clone <repository>
   cd hospital-microservices
   ```

2. **Create environment file**
   ```bash
   cp .env.example .env
   ```
   Update `.env` with your credentials (database passwords, API keys, etc.)

3. **Start all services**
   ```bash
   # Start infra first (slow images — Keycloak, Elasticsearch, SQL Server)
   docker-compose up -d postgres mongo sqlserver rabbitmq redis elasticsearch keycloak prometheus grafana seq

   # Once infra is healthy (~60s), start application services
   docker-compose up -d hospital-gateway patient-service appointment-service \
     medical-record-service pharmacy-service notification-service search-service
   ```

4. **Verify services are healthy**
   ```bash
   # Check all containers running
   docker-compose ps

   # Run smoke tests
   bash tests/smoke/smoke-test.sh
   ```

5. **Access services**
   | Service | URL | Purpose |
   |---|---|---|
   | Gateway | http://localhost:8000 | API entry point |
   | Patient Service | http://localhost:5001 | Patient management |
   | Appointment Service | http://localhost:5002 | Appointment scheduling |
   | Medical Record Service | http://localhost:5003 | Medical records |
   | Pharmacy Service | http://localhost:5004 | Drug inventory |
   | Notification Service | http://localhost:5005 | Email/SMS |
   | Search Service | http://localhost:5006 | Full-text search |
   | Keycloak Admin | http://localhost:8080 | Authentication config |
   | Prometheus | http://localhost:9090 | Metrics |
   | Grafana | http://localhost:3000 | Dashboards |
   | Seq | http://localhost:5341 | Logs |
   | Kibana | http://localhost:5601 | Log/data visualization (ELK) |
   | Logstash | localhost:5044 / 5000 | Log pipeline (Beats / TCP) |

---

## Project Structure

```
hospital-microservices/
├── docker-compose.yml              # Service orchestration
├── .env.example                    # Environment template
│
├── gateway/
│   └── HospitalGateway/            # YARP API Gateway (.NET 8)
│
├── services/
│   ├── PatientService/             # Patient CRUD (.NET 8, PostgreSQL)
│   ├── AppointmentService/         # Appointments (.NET 8, PostgreSQL)
│   ├── MedicalRecordService/       # Medical records (Node.js, MongoDB)
│   ├── PharmacyService/            # Drug inventory (Node.js, SQL Server)
│   ├── NotificationService/        # Notifications (Node.js, Redis)
│   └── SearchService/              # Full-text search (Node.js, Elasticsearch)
│
├── shared/
│   ├── HospitalShared/             # .NET shared library (DTOs, events)
│   └── hospital-shared-js/         # Node.js shared library (types, utils)
│
├── infra/
│   ├── keycloak/                   # Keycloak realm config
│   ├── prometheus/                 # Metrics scraping config
│   ├── grafana/                    # Dashboard definitions
│   └── scripts/                    # Database initialization
│
├── docs/                           # Documentation
│   ├── project-overview-pdr.md     # Project goals, PDR, timeline
│   ├── codebase-summary.md         # Codebase structure overview
│   ├── code-standards.md           # Coding conventions (.NET & Node.js)
│   ├── system-architecture.md      # Architecture, data flows, deployment
│   └── project-roadmap.md          # 6-phase implementation plan
│
├── tests/                          # Integration and load tests
│   ├── integration/
│   ├── load/
│   └── smoke/
│
└── README.md                       # This file
```

---

## Development Workflow

### Making Changes to Services

1. **Navigate to service directory**
   ```bash
   cd services/PatientService          # For .NET service
   cd services/MedicalRecordService    # For Node.js service
   ```

2. **Make code changes**
   - Follow standards in `docs/code-standards.md`
   - Write tests (target >80% coverage)
   - Run linting/formatting

3. **Build and test locally**
   ```bash
   # .NET
   dotnet build
   dotnet test

   # Node.js
   npm install
   npm test
   npm run lint
   ```

4. **Verify with Docker Compose**
   ```bash
   docker-compose up -d
   docker-compose logs -f patient-service
   ```

5. **Test via Gateway**
   ```bash
   curl -H "Authorization: Bearer {token}" http://localhost:8000/api/patients
   ```

### Running Tests

```bash
# Unit tests for a specific service
dotnet test services/PatientService/PatientService.Tests.csproj
npm test --prefix services/MedicalRecordService

# Integration tests
npm test --prefix tests/integration

# Load testing
k6 run tests/load/load-test.js
```

---

## Common Tasks

### Stopping All Services
```bash
docker-compose down
```

### Viewing Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f patient-service

# Last 100 lines of a specific service
docker-compose logs --tail=100 appointment-service
```

### Resetting Local Data
```bash
# Stop all services
docker-compose down

# Remove volumes (deletes all data)
docker-compose down -v

# Start fresh
docker-compose up -d
```

### Accessing Databases

**PostgreSQL:**
```bash
docker-compose exec postgres psql -U hospital -d hospital_db
# Inside psql: \dt patients; SELECT * FROM patients;
```

**MongoDB:**
```bash
docker-compose exec mongo mongosh
# Inside mongosh: use hospital_db; db.medical_records.find();
```

**SQL Server:**
```bash
docker-compose exec sqlserver sqlcmd -S localhost -U sa
# Inside sqlcmd: SELECT * FROM pharmacy.drugs;
```

### Generating Auth Token (Keycloak)

1. Open Keycloak admin console: http://localhost:8080
2. Login with admin credentials (from `.env`)
3. Navigate to realm "hospital" → clients → hospital-gateway
4. Generate token via "Service Account" tab or use OAuth2 client credentials flow

Alternatively, use script:
```bash
curl -X POST http://localhost:8080/realms/hospital/protocol/openid-connect/token \
  -d "client_id=hospital-gateway" \
  -d "client_secret={secret}" \
  -d "grant_type=client_credentials"
```

---

## API Examples

### Create Patient
```bash
curl -X POST http://localhost:8000/api/patients \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@hospital.com",
    "dateOfBirth": "1990-01-15",
    "phoneNumber": "+1-555-0100"
  }'
```

### Schedule Appointment
```bash
curl -X POST http://localhost:8000/api/appointments \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": "uuid-123",
    "providerId": "doctor-456",
    "scheduledTime": "2026-04-01T14:00:00Z",
    "duration": "PT30M"
  }'
```

### Search Patients
```bash
curl -X GET "http://localhost:8000/api/search?q=john&type=patient" \
  -H "Authorization: Bearer {token}"
```

---

## Documentation

| Document | Purpose |
|---|---|
| **[project-overview-pdr.md](docs/project-overview-pdr.md)** | Project vision, goals, requirements, PDR (Product Development Requirements) |
| **[codebase-summary.md](docs/codebase-summary.md)** | Codebase structure, service inventory, shared libraries, dependencies |
| **[code-standards.md](docs/code-standards.md)** | Coding conventions, architectural patterns, testing guidelines |
| **[system-architecture.md](docs/system-architecture.md)** | System design, data flows, deployment, monitoring, security |
| **[project-roadmap.md](docs/project-roadmap.md)** | 6-phase implementation plan, timeline, milestones |

---

## Key Technologies

### Backend
- **.NET 8** — Patient & Appointment services
- **Node.js 18+** — Medical Record, Pharmacy, Notification, Search services
- **Express.js** — HTTP framework for Node.js services
- **Entity Framework Core** — ORM for .NET
- **Mongoose** — MongoDB ODM for Node.js

### Databases
- **PostgreSQL 15** — Transactional data (Patient, Appointment)
- **MongoDB 6** — Document storage (Medical Records)
- **SQL Server 2022** — Pharmacy data
- **Elasticsearch 8** — Full-text search
- **Redis 7** — Caching, rate limiting, Pub/Sub

### Infrastructure & Messaging
- **Docker & Docker Compose** — Containerization
- **YARP 2.1** — API Gateway (.NET)
- **RabbitMQ 3.12** — Async messaging with MassTransit (.NET) and amqplib (Node.js)
- **Keycloak 21** — OAuth2/OIDC authentication
- **Prometheus 2.45** — Metrics collection
- **Grafana 10** — Metrics visualization
- **Seq 2023.4** — Structured logging

### Testing & Quality
- **xUnit** (.NET) — Unit testing
- **Jest** (Node.js) — Unit testing
- **Postman/REST Assured** — API testing
- **k6** — Load testing
- **ESLint** (Node.js) — Code linting
- **dotnet format** (.NET) — Code formatting

---

## Troubleshooting

### Services Won't Start
```bash
# Check for port conflicts
netstat -ano | findstr :8000    # Windows
lsof -i :8000                   # Mac/Linux

# Rebuild images without cache
docker-compose build --no-cache

# Clear volumes and restart
docker-compose down -v
docker-compose up -d
```

### Gateway Returns 503 (Service Unavailable)
- Verify downstream service is running: `docker-compose ps`
- Check service logs: `docker-compose logs patient-service`
- Verify service is healthy: `curl http://localhost:5001/health`
- Check YARP route configuration: `gateway/HospitalGateway/appsettings.json`

### JWT Token Validation Failing
- Verify Keycloak is running: `docker-compose ps keycloak`
- Check token expiration: Decode JWT at jwt.io
- Verify Keycloak URL in gateway: `KEYCLOAK_URL=http://keycloak:8080`
- Check token issuer matches `JWT_ISSUER` in gateway config

### RabbitMQ Messages Not Being Processed
- Verify RabbitMQ is running: `docker-compose ps rabbitmq`
- Access RabbitMQ management: http://localhost:15672 (guest/guest)
- Check queue depth and consumer count
- Verify subscription bindings: routing keys match event names
- Check service logs for error messages

### Database Connection Issues
- Verify database container is running: `docker-compose ps postgres`
- Check connection string in service config
- Test connection manually: `psql -h localhost -U hospital -d hospital_db`
- Verify credentials match `.env` file

---

## Performance Baseline

**Target metrics (at 1000 concurrent users):**
- Patient CRUD: <200ms (p95)
- Appointment scheduling: <300ms (p95)
- Patient search: <500ms (p95)
- Error rate: <0.1%
- System availability: 99.5%

---

## Contributing

1. **Read documentation** — Start with `docs/code-standards.md`
2. **Create feature branch** — `git checkout -b feature/your-feature`
3. **Write tests** — Aim for >80% coverage
4. **Follow conventions** — Naming, structure, error handling
5. **Lint and format** — `dotnet format` (.NET), `npm run lint` (Node.js)
6. **Submit PR** — Include tests, update docs if needed
7. **Code review** — Peer review before merge to main

---

## Deployment

### Development
- Local: Docker Compose (all services in containers)
- Each developer pulls latest `.env` and runs `docker-compose up -d`

### Staging
- Docker Swarm or Kubernetes
- Automated CI/CD pipeline: Push to branch → Build → Test → Deploy to staging
- Manual approval before production

### Production
- Kubernetes or Docker Swarm with load balancing
- Rolling updates (1 service at a time)
- Health checks and auto-rollback on failure

See `docs/system-architecture.md` for detailed deployment architecture.

---

## Support & Contact

- **Project Manager:** [TBD]
- **Tech Lead (.NET):** [TBD]
- **Tech Lead (Node.js):** [TBD]
- **DevOps Lead:** [TBD]

For issues, questions, or blockers:
1. Check documentation (`docs/` folder)
2. Search existing issues in repository
3. Escalate to relevant tech lead
4. For critical issues: Slack #hospital-hrm-dev

---

## License

[TBD — Specify license here]

---

## Release Notes

**Current Status:** Planning Phase (no releases yet)

See `docs/project-roadmap.md` for Phase 1 launch date and deliverables.

---

**Last Updated:** 2026-03-18
**Version:** 1.0 (Scaffold)

