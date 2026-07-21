# Phase 05 — Deployment & Integration

## Context Links
- Overview: [plan.md](plan.md); depends on [phase-04-api-controller.md](phase-04-api-controller.md)
- Templates: `docker-compose.patient-service.yml`, `services/OrchestratorService/Program.cs`

## Overview
- Priority: P2
- Status: pending
- Compose entry for FileService (port 5013) + optional Orchestrator routing + docs updates.

## Key Insights
- Compose per-service file pattern: dedicated network, healthcheck hitting `/health`, `restart: unless-stopped`.
- Orchestrator wires downstream via typed HttpClient keyed on `Services:FileService` config — only needed if BFF must proxy files (Q4).

## Requirements
- `docker-compose.file-service.yml` builds `services/FileService/Dockerfile`, maps `5013:5013`, env `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS=http://+:5013`, healthcheck on 5013.
- Docs updated per `documentation-management.md`.

## Related Code Files
Create:
- `docker-compose.file-service.yml`
Modify (optional, Q4):
- `services/OrchestratorService/Program.cs` (+ appsettings) — add `Services:FileService` typed client / route
Modify (docs):
- `docs/system-architecture.md`, `docs/project-changelog.md`, `docs/development-roadmap.md`

## Implementation Steps
1. Copy patient compose → file-service compose; rename service/container/network to `file-service`; ports 5013; healthcheck port 5013.
2. (Optional Q4) Register FileService client + route in Orchestrator mirroring PatientService block; add `Services:FileService` default `http://file-service:5013`.
3. Update docs: add FileService to architecture (port 5013, in-DB storage), changelog entry, roadmap status.
4. `docker compose -f docker-compose.file-service.yml build` sanity (optional).

## Todo List
- [ ] docker-compose.file-service.yml
- [ ] (optional) Orchestrator route + config
- [ ] docs updated (architecture/changelog/roadmap)

## Success Criteria
- Compose builds & container healthy on 5013; docs reflect new service.

## Risk Assessment
- Port clash on host. Mitigate: 5013 confirmed unused.
- Compose healthcheck path mismatch. Mitigate: `/health` exists (phase 1).

## Security Considerations
- Do not commit secrets; reuse existing env/connection-string conventions.

## Next Steps
- Phase 06 testing & review.
