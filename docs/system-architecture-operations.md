# Hospital HRM Microservices — System Architecture (Operations)

Part 2 of system architecture. See [system-architecture.md](./system-architecture.md) for Part 1.

---

## Data Consistency & Eventual Consistency

**Strong Consistency (Synchronous):**
- Patient CRUD operations (single database)
- Appointment scheduling (validate patient via HTTP)

**Eventual Consistency (Asynchronous):**
- Patient indexed in Elasticsearch (delay: <5s)
- Medical record created after appointment (delay: <5s)
- Search index updated (delay: <5s)

**Tolerance:** All non-critical data reaches consistency within 60 seconds.

---

## Caching Strategy

### Redis Usage

| Key Pattern | TTL | Purpose |
|---|---|---|
| `patient:{patientId}` | 3600s (1h) | Patient profile cache |
| `appointment:{appointmentId}` | 1800s (30m) | Appointment details |
| `availability:{providerId}:{date}` | 600s (10m) | Available time slots |
| `search:recent:{userId}` | 3600s | User's recent searches |
| `rate-limit:{userId}` | 60s | Rate limiting counter |

**Invalidation:**
- Manual: On write operations (update patient → invalidate `patient:{id}`)
- TTL: Automatic expiration
- Event-driven: Subscribe to update events, invalidate cache

---

## Security Architecture

### Authentication & Authorization

```
Client HTTP Request + JWT Token
    ↓
Gateway (YARP)
    ├─ Extract token from Authorization header
    ├─ Validate signature (Keycloak public key)
    ├─ Check token expiration
    ├─ Extract claims (user ID, roles)
    └─ Forward to service with User principal

Service Receives
    ├─ User ID from token claim
    ├─ Roles/permissions from token claims
    └─ Validates access (service-level authorization)
```

**Token:**
```json
{
  "sub": "user-123",
  "email": "doctor@hospital.com",
  "realm_access": {
    "roles": ["doctor", "patient-reader"]
  },
  "exp": 1710772800,
  "iat": 1710768400
}
```

### Inter-Service Security

- **Keycloak:** OAuth2/OIDC issuer for tokens
- **HTTPS/TLS 1.3:** All inter-service communication (in production)
- **Service Account:** Optional: Service-to-service calls use separate service accounts (client credentials flow)

### Data Protection

- **Encryption at Rest:** PostgreSQL TDE, MongoDB encryption
- **Encryption in Transit:** TLS 1.3 for all HTTP, RabbitMQ AMQPS
- **Secrets:** Environment variables or vault (Keycloak, DB passwords, API keys)

---

## Monitoring & Observability

### Logging

**Centralized to Seq (port 5341):**
```json
{
  "timestamp": "2026-03-18T10:30:45Z",
  "level": "Information",
  "message": "Patient created",
  "patientId": "uuid-123",
  "email": "john@hospital.com",
  "service": "patient-service",
  "correlationId": "req-abc-123-def",
  "duration": 125
}
```

**Log Levels:**
- `Trace` — Detailed diagnostic info (disabled in production)
- `Debug` — Development-level info (disabled in production)
- `Information` — General operational info (enabled)
- `Warning` — Potential issues (enabled)
- `Error` — Errors with recovery (enabled)
- `Critical` — System-level failures (enabled)

### Metrics (Prometheus)

**Scrape Interval:** 15s from all services

**Key Metrics:**
```
# Request metrics
http_requests_total{service="patient-service", method="POST", endpoint="/patients", status="201"}
http_request_duration_seconds{service="patient-service", quantile="0.95"}

# Database metrics
db_connection_pool_size{service="patient-service"}
db_query_duration_seconds{service="patient-service", query="GetPatient"}

# Business metrics
patients_created_total
appointments_scheduled_total
prescriptions_issued_total
```

### Tracing (OpenTelemetry)

**Trace Flow Example:**
```
GET /api/patients/123
  ├─ trace_id: abc-123-def
  ├─ span: gateway-jwt-validation (10ms)
  ├─ span: route-to-service (2ms)
  ├─ span: patient-service-handler (50ms)
  │  ├─ span: database-query (35ms)
  │  └─ span: redis-cache-update (10ms)
  └─ span: response-serialize (3ms)
  └─ Total: 65ms
```

### Dashboard (Grafana)

**Pre-built Dashboards:**
- **System Health:** Uptime, error rates, latency (p50, p95, p99)
- **Service Metrics:** Per-service request volume, response times, errors
- **Database:** Connection pool, query times, lock waits
- **RabbitMQ:** Message throughput, queue depth, DLQ messages
- **Business Metrics:** Patients created, appointments, prescriptions

---

## Deployment Architecture

### Container Orchestration

**Environment:** Docker Compose (dev), Docker Swarm or Kubernetes (production)

**Services per Container:**
```yaml
gateway:           # HospitalGateway
  image: hospital/gateway:latest
  ports: "8000:8000"

patient-service:   # PatientService
  image: hospital/patient-service:latest
  ports: "5001:5001"

medical-record-service:  # MedicalRecordService
  image: hospital/medical-record-service:latest
  ports: "3001:3001"

# ... additional services ...

postgres:
  image: postgres:15
  environment:
    POSTGRES_DB: hospital_db
    POSTGRES_USER: hospital
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  volumes: ["pgdata:/var/lib/postgresql/data"]

mongodb:
  image: mongo:6
  environment:
    MONGO_INITDB_DATABASE: hospital_db
  volumes: ["mongodata:/data/db"]

rabbitmq:
  image: rabbitmq:3.12-management
  ports: ["5672:5672", "15672:15672"]

redis:
  image: redis:7
  ports: ["6379:6379"]

keycloak:
  image: quay.io/keycloak/keycloak:21
  ports: ["8080:8080"]
  environment:
    KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN}
    KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
```

### High Availability

**Redundancy:**
- Multiple instances per service (load balancer, round-robin)
- Database replication (PostgreSQL streaming replication)
- RabbitMQ cluster (3+ nodes)
- Redis cluster or sentinel (high availability)

**Health Checks:**
```
GET /health → 200 OK
{
  "status": "healthy",
  "checks": {
    "database": "ok",
    "rabbitmq": "ok",
    "redis": "ok"
  }
}
```

---

## Disaster Recovery

| Scenario | RTO | RPO | Strategy |
|---|---|---|---|
| Service crash | 5 min | 0 | Auto-restart, health checks |
| Database failure | 1 hour | 1 hour | Automated backup, standby replica |
| Message broker outage | 15 min | 5 min | RabbitMQ clustering, persistence |
| Data center loss | 4 hours | 1 hour | Geo-replicated backups, failover |

**Backup:**
- Daily automated PostgreSQL backups (retained 30 days)
- MongoDB point-in-time recovery (24-hour window)
- Redis snapshots (daily, 7-day retention)

---

## Version Control & Deployment

**Git Repository Structure:**
```
hospital-microservices/
├── gateway/                 # Branch: main → CI/CD → Docker image
├── services/patient/        # Branch: main → CI/CD → Docker image
├── services/appointment/    # ... etc
├── docker-compose.yml       # Orchestration
└── docs/                    # Documentation
```

**CI/CD Pipeline:**
1. Push to `main` branch
2. Run linting, unit tests
3. Build Docker image
4. Push to registry (e.g., Docker Hub)
5. Deploy to staging (automated)
6. Run integration/smoke tests
7. Await approval for production deployment
8. Deploy to production (rolling update, 1 service at a time)

---

## Scaling Considerations

### Horizontal Scaling (Services)

```
Load Balancer (e.g., Docker Swarm, Kubernetes Service)
    ├─ patient-service:1 (5001)
    ├─ patient-service:2 (5001)
    └─ patient-service:3 (5001)

Each instance has its own:
    ├─ Connection pool to PostgreSQL
    ├─ Redis client
    └─ RabbitMQ consumer group
```

### Vertical Scaling (Databases)

- **PostgreSQL:** Add read replicas for read-heavy workloads
- **MongoDB:** Sharding by patientId for large collections
- **Elasticsearch:** Index sharding, data tiering
- **Redis:** Cluster mode (distribute keys across nodes)

### Message Broker Scaling

- **RabbitMQ:** Consumer groups (multiple nodes consuming same queue)
- **Partitioning:** Topic-based (fanout to all, or queue-based for load distribution)

---

## Known Limitations & Future Enhancements

- [ ] **Multi-tenancy:** Not in v1.0 scope; each deployment is single-hospital
- [ ] **Graphical Data:** Image/PDF handling via S3; local storage for dev only
- [ ] **Real-time Notifications:** WebSockets not included; polling or email/SMS only
- [ ] **HIPAA Compliance:** Architectural compliance only; security audit required
- [ ] **API Versioning:** Strategy (v1, v2) to be finalized before API is public
- [ ] **Rate Limiting:** Per-user; can be enhanced with per-IP, per-role later
- [ ] **Audit Trail:** Event-based; detailed audit log design in Phase 6

---

## Glossary

| Term | Definition |
|---|---|
| **YARP** | Yet Another Reverse Proxy — Microsoft's .NET-based API gateway |
| **CQRS** | Command Query Responsibility Segregation — separate read/write operations |
| **RTO** | Recovery Time Objective — max downtime tolerable |
| **RPO** | Recovery Point Objective — max data loss tolerable |
| **DLQ** | Dead Letter Queue — holds messages that failed processing |
| **TTL** | Time To Live — cache expiration time |
| **TLE** | Transparent Data Encryption — database encryption at rest |

