# Hospital HRM Microservices — Project Overview & PDR

## Project Vision

Build a **scalable, modular Hospital Resource Management (HRM) system** using microservices architecture. The system enables hospitals to manage patients, appointments, medical records, pharmacy operations, and notifications through decoupled services that communicate via HTTP and async messaging.

---

## Project Goals

1. **Scalability** — Each service scales independently based on demand
2. **Maintainability** — Clear service boundaries reduce complexity
3. **Technology Flexibility** — Use best-fit tech per service (.NET for core, Node.js for auxiliary)
4. **Observability** — Distributed logging, monitoring, and tracing across all services
5. **Security** — Centralized authentication (Keycloak), JWT validation, secure inter-service communication
6. **Performance** — Async messaging for non-blocking operations, caching where applicable
7. **Resilience** — Circuit breakers, retry policies, health checks, graceful degradation

---

## Stakeholders

| Role | Responsibility |
|---|---|
| **Hospital Admin** | Monitor system health, manage infrastructure, approve deployments |
| **Clinical Staff** | Use patient, appointment, and medical record features |
| **Pharmacy Team** | Manage drug inventory, process prescriptions |
| **IT Operations** | Deploy, monitor, troubleshoot infrastructure and services |
| **Development Team** | Design, implement, test, and maintain services |

---

## Key Requirements

### Functional Requirements

**Patient Management**
- Create, read, update, delete patient profiles
- Store patient demographics, contact info, medical history
- Support bulk operations for data import

**Appointment Management**
- Schedule, reschedule, cancel appointments
- Check appointment availability by provider and date
- Link appointments to patient records
- Validate appointment conflicts

**Medical Records**
- Store medical records, lab results, diagnoses
- Retrieve records by patient or date range
- Audit trail for all record changes
- Support document uploads (PDF, images)

**Pharmacy Operations**
- Manage drug inventory, pricing, expiration dates
- Process prescriptions from appointments
- Track drug usage and stock levels
- Support drug lookups by name or code

**Notifications**
- Send SMS/Email appointment reminders
- Notify clinical staff of lab results
- Alert pharmacy of new prescriptions
- Configurable notification templates

**Search & Discovery**
- Full-text search for patients, drugs
- Filter by multiple criteria
- Real-time indexing

### Non-Functional Requirements

| Requirement | Target |
|---|---|
| **Availability** | 99.5% uptime |
| **Response Time** | <500ms p95 for patient queries |
| **Throughput** | 1000+ concurrent users |
| **Data Retention** | 7+ years for medical records |
| **Backup** | Daily automated backups |
| **Disaster Recovery** | RTO 4 hours, RPO 1 hour |
| **Audit Trail** | 100% for medical records, compliance-ready |
| **Security** | TLS 1.3, encrypted at rest, HIPAA-aligned |

---

## Architecture Highlights

**Technology Stack:**
- **API Gateway:** YARP (.NET 8) — routing, rate limiting, JWT validation
- **.NET 8 Services:** Patient, Appointment, Auth services with Entity Framework Core
- **Node.js Services:** Medical Record, Pharmacy, Notification, Search services with Express
- **Databases:** PostgreSQL (transactional), MongoDB (documents), SQL Server (pharmacy), Elasticsearch (search)
- **Async Messaging:** RabbitMQ with MassTransit (.NET) and amqplib (Node.js)
- **Caching & Pub/Sub:** Redis
- **IAM:** Keycloak for OAuth2/OIDC
- **Monitoring:** Prometheus + Grafana + Seq (logging) + OpenTelemetry (tracing)

**Communication Patterns:**
- Synchronous: HTTP/REST via gateway, gRPC for performance-critical internal calls
- Asynchronous: RabbitMQ events for non-blocking operations, eventual consistency

---

## Success Criteria

### Development Success
- All 7 services deployed to Docker containers
- Shared libraries (.NET + Node.js) reduce code duplication by >30%
- All services communicate via gateway and async messaging
- Zero code duplication across services

### Operational Success
- Prometheus dashboard shows all services healthy
- Logs centralized in Seq with structured tracing
- Backup jobs complete daily without errors
- Alert thresholds configured and tested

### Functional Success
- Patient CRUD operations respond <200ms
- Appointment scheduling <300ms end-to-end
- Notifications sent within 60s of event trigger
- Search query results <500ms for 100k+ patient database

### Quality Success
- Unit test coverage >80% for all services
- Integration tests for all async flows
- Load test validates 1000 concurrent users
- Security audit clearance (HTTPS, encryption, auth)

---

## Constraints & Assumptions

**Constraints:**
- Must support .NET 8 and Node.js 18+ only (no legacy versions)
- All services must be containerized (Docker)
- On-premises deployment required (no cloud-only dependencies)
- Compliance: HIPAA-aligned (not full HIPAA, but healthcare-grade security)

**Assumptions:**
- Development team has Docker, .NET 8, and Node.js experience
- Keycloak instance already available or will be provisioned
- PostgreSQL, MongoDB, SQL Server, RabbitMQ can be self-hosted or cloud-managed
- Internet bandwidth sufficient for real-time notifications
- On-call support available 24/7 for production issues

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|---|---|---|
| Service coupling through events | High latency, cascading failures | Dead letter queues, retry policies, circuit breakers |
| Database contention | Slow queries, locks | Read replicas, caching, indexed queries |
| Auth token expiration | User lockouts | Refresh token strategy, auto-renewal |
| Message broker outage | Lost notifications | RabbitMQ clustering, persistence |
| Search index lag | Stale data | Real-time indexing via events, eventual consistency window |

---

## Timeline & Phases

| Phase | Duration | Focus | Output |
|---|---|---|---|
| Phase 1 | Week 1 | Infrastructure | docker-compose, networks, volumes |
| Phase 2 | Week 2 | Gateway + Auth | YARP routing, JWT validation, rate limiting |
| Phase 3 | Weeks 3-4 | Core .NET Services | Patient + Appointment services + events |
| Phase 4 | Weeks 5-6 | Node.js Services | Medical Record, Pharmacy + event handlers |
| Phase 5 | Week 7 | Notification + Search | Full notification pipeline + Elasticsearch |
| Phase 6 | Week 8 | Observability + Polish | Logging, monitoring, tracing, production-ready |

---

## Next Steps

1. Approve architecture and tech stack
2. Provision infrastructure (dev, staging, production environments)
3. Create shared libraries (.NET DTOs, Node.js types)
4. Begin Phase 1 (infrastructure setup)
5. Schedule weekly sync with stakeholders

---

## Document Control

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | 2026-03-18 | Architecture Team | Initial PDR |

---

**Project Start Date:** 2026-03-18
**Estimated Completion:** 2026-05-13
**Project Manager:** [TBD]
**Tech Lead:** [TBD]

