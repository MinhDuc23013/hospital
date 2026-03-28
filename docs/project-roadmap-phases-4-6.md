# Hospital HRM Microservices — Project Roadmap (Phases 4-6)

Part 2 of roadmap. See [project-roadmap.md](./project-roadmap.md) for Phases 1-3.

---

## Phase 5: Notification + Search Refinement (Week 7)

**Objective:** Complete notifications and search services, add templates, enhance observability.

### Overview
- Finalize notification templates and multi-channel delivery
- Optimize Elasticsearch indexes and search relevance
- Add notification logging and audit trail
- Integration testing for all async flows
- Load testing (1000 concurrent users)

### Priority
MEDIUM — Enhance user experience, ensure reliability

### Key Deliverables

1. **Notification Templates**
   - Appointment reminder: "Your appointment with Dr. Smith is on {date} at {time}"
   - Welcome email: "Welcome to Hospital HRM, {patientName}!"
   - Lab result: "Your lab results are ready. Please contact your doctor."
   - Pharmacy alert: "Low stock alert for {drugName}. Current: {quantity}"

2. **Multi-Channel Support**
   - Email: Nodemailer + SMTP
   - SMS: Twilio API
   - Push (optional): Firebase Cloud Messaging
   - In-app notifications (optional)

3. **Search Relevance**
   - Tune Elasticsearch scoring (name vs. email relevance)
   - Add autocomplete for patient names
   - Test search with 100k+ patient dataset

4. **Audit Trail**
   - Log all notifications sent: timestamp, recipient, template, channel, status
   - Store in MongoDB collection `notification_logs`
   - API: GET /api/notifications/logs?patientId={id}&limit=50

5. **Integration Tests**
   - End-to-end: Patient creation → welcome email sent
   - End-to-end: Appointment scheduled → SMS reminder sent
   - End-to-end: Prescription issued → email to patient, alert to pharmacy

6. **Load Testing**
   - k6 or JMeter: 1000 concurrent users, 100 RPS
   - Target: p95 response time <500ms for GET /api/patients
   - Identify bottlenecks, optimize

### Success Criteria
- [ ] All notification templates defined and tested
- [ ] Email delivery: <5s after event
- [ ] SMS delivery: <5s after event
- [ ] Notification audit trail stored in MongoDB
- [ ] Search works with 100k patients, results in <500ms
- [ ] Load test: 1000 concurrent users, <2% error rate
- [ ] All integration tests pass
- [ ] Documentation updated with template management

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Email provider rate limit | Medium | Medium | Queue with rate limiting, monitor quota |
| SMS cost overrun | Low | High | Set daily/monthly limits, test with test account |
| Elasticsearch response time slow | Low | High | Index tuning, sharding, caching |

### Dependencies
- Phase 4: Notification and Search services running

### Next Steps
- User acceptance testing: Verify notifications and search from user perspective
- Transition to Phase 6 (Observability + Release)

---

## Phase 6: Observability + Production Release (Week 8)

**Objective:** Complete logging, monitoring, tracing setup. Prepare for production deployment.

### Overview
- Centralized logging (Seq) with structured logs
- Prometheus metrics and Grafana dashboards
- OpenTelemetry distributed tracing
- Security hardening (HTTPS, secrets management)
- Performance optimization and tuning
- Documentation completion

### Priority
HIGH — Required for production readiness

### Key Deliverables

1. **Structured Logging (Seq)**
   - All services log to Seq (http://seq:5341)
   - Correlation ID in all logs (trace requests across services)
   - Log format: timestamp, level, service, message, context (user, patientId, etc.)

2. **Prometheus Metrics**
   - All services expose `/metrics` endpoint
   - Custom metrics: `patients_created_total`, `appointments_scheduled_total`, `notifications_sent_total`
   - System metrics: HTTP requests, database queries, RabbitMQ messages

3. **Grafana Dashboards**
   - System Health: Uptime, error rates, response times (p50, p95, p99)
   - Service Metrics: Per-service request volume, latency, error rate
   - Database: Connection pool, query times, slow queries
   - RabbitMQ: Message throughput, queue depth, DLQ size
   - Business Metrics: Patients, appointments, prescriptions created

4. **OpenTelemetry Distributed Tracing**
   - Instrument .NET services (OpenTelemetry.Instrumentation.AspNetCore)
   - Instrument Node.js services (TracingMiddleware)
   - Trace exports to Seq or separate OTEL collector
   - Sample: 10% of requests for cost control

5. **Security Hardening**
   - Enable HTTPS/TLS 1.3 (self-signed certs for dev, proper certs for prod)
   - Rotate secrets (Keycloak client secret, DB passwords)
   - Remove default credentials (RabbitMQ guest user)
   - Enable database encryption at rest
   - Validate all inputs, prevent SQL injection

6. **Performance Optimization**
   - Profile services under load, identify bottlenecks
   - Optimize database queries (add indexes, adjust query plans)
   - Tune connection pools, thread counts
   - Cache frequently accessed data (Redis)
   - Implement pagination defaults (50 items/page)

7. **Documentation Completion**
   - API documentation (OpenAPI/Swagger)
   - Deployment runbook
   - Troubleshooting guide
   - On-call guide

### Success Criteria
- [ ] All services log to Seq with correlation IDs
- [ ] Prometheus scrapes all metrics successfully
- [ ] Grafana dashboards display real-time data
- [ ] Distributed traces show full request flow across services
- [ ] HTTPS enabled (self-signed for dev)
- [ ] Secrets not in code or logs
- [ ] Load test results: <500ms p95, <2% errors @ 1000 concurrent
- [ ] Documentation complete and reviewed
- [ ] Security audit passed (code review + pen test plan)

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Performance regression discovered during load test | Medium | High | Early load testing in Phase 5, continuous profiling |
| Secrets leaked in logs | Medium | High | Log redaction, scan logs for secrets |
| HTTPS cert expiration | Low | High | Automated renewal, monitoring |
| Observability overhead slows services | Low | High | Sampling, async logging, profile overhead |

### Dependencies
- Phases 1-5: All services running and integrated

### Next Steps
- Final security review and audit
- User acceptance testing (UAT)
- Preparation for production deployment

---

## Success Metrics & KPIs

### Functional Success

| Metric | Target | Measurement |
|---|---|---|
| **Patient CRUD latency (p95)** | <200ms | Average response time across 1000 requests |
| **Appointment scheduling latency** | <300ms | End-to-end from request to response |
| **Notification delivery time** | <5s | Time from event publish to email/SMS sent |
| **Search query latency** | <500ms | Query time for "john" across 100k patients |
| **Medical record creation (on appointment)** | <5s | Time from appointment event to record in DB |

### Reliability Success

| Metric | Target | Measurement |
|---|---|---|
| **System availability** | 99.5% | Uptime across all services (measured monthly) |
| **Service error rate** | <0.1% | 4xx + 5xx / total requests |
| **RabbitMQ message delivery** | 100% | No lost messages, confirmed delivery |
| **Database backup success rate** | 100% | Daily automated backups complete successfully |

### Quality Success

| Metric | Target | Measurement |
|---|---|---|
| **Unit test coverage** | >80% | Code coverage across all services |
| **Integration test pass rate** | 100% | All integration tests passing |
| **Security vulnerabilities** | 0 critical | Dependency scanning, code review |
| **Documentation completeness** | 100% | All services documented with examples |

---

## Milestones & Review Gates

### Milestone 1: Infrastructure Ready (End of Week 1)
- [ ] All containers run successfully
- [ ] Databases initialized, seed data loaded
- [ ] All services reachable on expected ports
- **Gate:** Proceed to Phase 2 ✅

### Milestone 2: API Gateway Live (End of Week 2)
- [ ] Gateway routes all service requests
- [ ] JWT validation working
- [ ] Rate limiting functional
- **Gate:** Proceed to Phase 3 ✅

### Milestone 3: Core Services Deployed (End of Week 4)
- [ ] Patient and Appointment services running
- [ ] Events published to RabbitMQ
- [ ] 80%+ unit test coverage
- **Gate:** Proceed to Phase 4 ✅

### Milestone 4: All Services Running (End of Week 6)
- [ ] All 7 services deployed
- [ ] Event handlers processing correctly
- [ ] End-to-end flows working (patient → appointment → notification)
- **Gate:** Proceed to Phase 5 ✅

### Milestone 5: Production Ready (End of Week 8)
- [ ] All observability (logging, metrics, tracing) live
- [ ] Load testing passed (1000 concurrent users)
- [ ] Security audit passed
- [ ] Documentation complete
- **Gate:** Approve for production deployment ✅

---

## Resource Allocation

| Role | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Phase 5 | Phase 6 |
|---|---|---|---|---|---|---|
| Infrastructure Lead | 5d | 2d | 0d | 0d | 2d | 1d |
| .NET Lead | 1d | 5d | 10d | 0d | 1d | 2d |
| Node.js Lead | 1d | 1d | 0d | 10d | 3d | 2d |
| QA Lead | 1d | 1d | 3d | 3d | 3d | 3d |
| DevOps | 2d | 1d | 1d | 1d | 2d | 3d |

---

## Communication Plan

### Weekly Sync
- **Day:** Every Monday 10:00 AM
- **Duration:** 30 minutes
- **Participants:** Project Manager, Tech Leads, QA Lead
- **Agenda:** Status by phase, blockers, risk review

### Status Reports
- **Frequency:** Friday EOD
- **Format:** Phase status (on track / at risk / blocked), completed deliverables, next week's focus

### Escalation
- **Blockers:** Escalate immediately via Slack + email
- **Risks:** Discuss in weekly sync, update roadmap if needed

---

## Contingency Planning

### If Phase Slips 1 Week
- Reduce scope of lowest-priority features (e.g., advanced search filters, push notifications)
- Compress Phase 5 and Phase 6 (reduce load testing duration)

### If Critical Bug Found in Production
- Allocate 1-2 engineers immediately to investigate and fix
- Delay next Phase start by 3-5 days
- Update roadmap with post-release stabilization phase

### If Key Team Member Unavailable
- Cross-train backup team member
- Redistribute workload
- Adjust timeline if necessary

---

## Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | 2026-03-18 | Project Team | Initial roadmap |

---

## Appendix: Phase Dependencies

```
Phase 1 (Infrastructure)
    ↓
Phase 2 (Gateway + Auth)
    ↓
Phase 3 (Core .NET Services)
    ↓
Phase 4 (Node.js Services)  [can start while Phase 3 finishing]
    ↓
Phase 5 (Notifications + Search)
    ↓
Phase 6 (Observability + Release)
```

**Parallel Work Possible:** Phase 3 and Phase 4 can overlap in final week of Phase 3.

---

