---
title: "Initialize Hospital HRM Microservices Project"
description: "Scaffold entire microservices workspace: Docker, infra, shared libs, gateway, 6 services"
status: completed
priority: P1
effort: 16h
branch: main
tags: [init, scaffold, microservices, docker, dotnet, nodejs]
created: 2026-03-18
completed: 2026-03-18
---

# Hospital HRM Microservices — Project Initialization Plan

## Objective

Scaffold the entire Hospital HRM project from empty workspace. Create directory structure, Docker Compose, infra configs, shared libraries, API gateway, and all 6 microservices with health endpoints and basic CRUD skeletons.

## Execution Strategy

```
Phase 1 (Foundation) ──────────────────────────────┐
  docker-compose, .env, infra, shared libs          │
                                                     │
  ┌──────────────────────────────────────────────────┤
  │ PARALLEL GROUP (after Phase 1)                   │
  │                                                  │
  │  Phase 2: Gateway + Keycloak config              │
  │  Phase 3: PatientService + AppointmentService    │
  │  Phase 4: MedicalRecordService + PharmacyService │
  │  Phase 5: NotificationService + SearchService    │
  │                                                  │
  └──────────────────────────────────────────────────┤
                                                     │
Phase 6 (Integration) ────────────────────────────────
  Wire docker-compose overrides, smoke tests, README
```

## Phase Summary

| Phase | File | Status | Progress | Effort | Depends On |
|-------|------|--------|----------|--------|------------|
| 1 | [phase-01-foundation.md](./phase-01-foundation.md) | Completed | 100% | 4h | None |
| 2 | [phase-02-gateway-auth.md](./phase-02-gateway-auth.md) | Completed | 100% | 2h | Phase 1 |
| 3 | [phase-03-dotnet-core-services.md](./phase-03-dotnet-core-services.md) | Completed | 100% | 3h | Phase 1 |
| 4 | [phase-04-nodejs-services-part1.md](./phase-04-nodejs-services-part1.md) | Completed | 100% | 3h | Phase 1 |
| 5 | [phase-05-nodejs-services-part2.md](./phase-05-nodejs-services-part2.md) | Completed | 100% | 2h | Phase 1 |
| 6 | [phase-06-integration.md](./phase-06-integration.md) | Completed | 100% | 2h | Phases 2-5 |

## File Ownership Matrix

Each phase owns exclusive files. No overlapping edits allowed.

| Phase | Owned Paths |
|-------|-------------|
| 1 | `docker-compose.yml`, `.env.example`, `.gitignore`, `.editorconfig`, `infra/**`, `shared/HospitalShared/**`, `shared/hospital-shared-js/**` |
| 2 | `gateway/HospitalGateway/**`, `infra/keycloak/**` |
| 3 | `services/PatientService/**`, `services/AppointmentService/**` |
| 4 | `services/MedicalRecordService/**`, `services/PharmacyService/**` |
| 5 | `services/NotificationService/**`, `services/SearchService/**` |
| 6 | `docker-compose.override.yml`, `tests/**`, updates to `docker-compose.yml` (service entries only) |

## Key Constraints

- Scaffold only: health endpoints, basic CRUD skeletons, no production business logic
- No secrets in committed files; use `.env.example` pattern
- .NET services: Clean Architecture (Controllers/Application/Domain/Infrastructure)
- Node.js services: Express with controller/service/model layers, kebab-case files
- Each service gets its own Dockerfile
- YAGNI/KISS/DRY throughout

## Dependency Graph

```
Phase 1 (Foundation)
    |
    +---> Phase 2 (Gateway + Auth)         ──┐
    +---> Phase 3 (.NET Core Services)     ──┤
    +---> Phase 4 (Node.js Part 1)         ──├──> Phase 6 (Integration)
    +---> Phase 5 (Node.js Part 2)         ──┘
```

## Port Allocation

| Service/Infra | Port |
|---|---|
| HospitalGateway | 8000 |
| PatientService | 5001 |
| AppointmentService | 5002 |
| MedicalRecordService | 5003 |
| PharmacyService | 5004 |
| NotificationService | 5005 |
| SearchService | 5006 |
| PostgreSQL | 5432 |
| MongoDB | 27017 |
| SQL Server | 1433 |
| RabbitMQ AMQP | 5672 |
| RabbitMQ UI | 15672 |
| Redis | 6379 |
| Elasticsearch | 9200 |
| Keycloak | 8080 |
| Prometheus | 9090 |
| Grafana | 3000 |
| Seq | 5341 |

## Validation Log

### Session 1 — 2026-03-18
**Trigger:** Post-plan validation interview
**Questions asked:** 7

#### Questions & Answers

1. **[Architecture]** Node.js services — TypeScript or plain JavaScript?
   - Options: TypeScript | Plain JavaScript
   - **Answer:** TypeScript
   - **Rationale:** Type safety aligns with hospital-shared-js types; affects tsconfig, ts-node, build scripts in all Node.js service phases.

2. **[Architecture]** How should hospital-shared-js be linked during local dev?
   - Options: npm link / file: protocol | Turborepo/Nx | Local registry
   - **Answer:** npm link / file: protocol (`"hospital-shared-js": "file:../../shared/hospital-shared-js"`)
   - **Rationale:** Simplest approach; no extra tooling; affects package.json in phases 4 & 5.

3. **[Scope]** Keycloak realm — working JSON import or placeholder?
   - Options: Working realm JSON | Placeholder + manual docs
   - **Answer:** Working realm JSON import
   - **Rationale:** Auto-loaded on first boot via docker-compose volume; affects phase 2 (infra/keycloak/).

4. **[Architecture]** MassTransit wiring in scaffold?
   - Options: Wire in DI (empty consumers/publishers) | Comment stub only
   - **Answer:** Wire MassTransit in DI
   - **Rationale:** Proper DI registration in Program.cs; affects phase 3 .NET services.

5. **[Scope]** DB migration/seed scripts in scaffold?
   - Options: Yes, include init migrations | Skip for now
   - **Answer:** Yes, include init migrations
   - **Rationale:** EF Core initial migration + Mongoose index setup so services can boot clean; affects phases 3 & 4.

6. **[Architecture]** .NET service architecture pattern?
   - Options: Clean Architecture + CQRS/MediatR | Minimal API + Service layer
   - **Answer:** Clean Architecture + CQRS/MediatR
   - **Rationale:** Matches code-standards.md; already planned in phase 3 — confirmed.

7. **[Architecture]** Git structure?
   - Options: Single root .gitignore | Per-service .gitignore
   - **Answer:** Single .gitignore at root
   - **Rationale:** Simpler monorepo management; affects phase 1.

#### Confirmed Decisions
- TypeScript for all Node.js services — affects phases 4, 5
- file: protocol for hospital-shared-js linking — affects phases 4, 5
- Working Keycloak realm JSON — affects phase 2
- MassTransit wired in DI — affects phase 3
- EF Core migrations + Mongoose index init included — affects phases 3, 4
- Clean Architecture + CQRS/MediatR confirmed — phase 3 already correct
- Single root .gitignore — affects phase 1

#### Action Items
- [x] Phase 1: Single root .gitignore (Node.js + .NET + Docker patterns)
- [x] Phase 2: infra/keycloak/realm-export.json pre-configured realm
- [x] Phase 3: MassTransit registered in DI + EF Core initial migration
- [x] Phase 4: TypeScript + tsconfig + ts-node + file: references + Mongoose index init
- [x] Phase 5: TypeScript + tsconfig + ts-node + file: references

#### Impact on Phases
- Phase 1: Add comprehensive root .gitignore covering .NET, Node.js, Docker
- Phase 2: Add infra/keycloak/realm-export.json with hospital realm, client, roles
- Phase 3: Wire MassTransit in Program.cs; add `dotnet ef migrations add Initial`
- Phase 4: TypeScript setup (tsconfig.json, ts-node, nodemon); hospital-shared-js via file:; Mongoose createIndexes()
- Phase 5: TypeScript setup (tsconfig.json, ts-node); hospital-shared-js via file:
