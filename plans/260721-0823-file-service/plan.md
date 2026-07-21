---
title: "FileService — file upload & DB storage microservice"
description: "New .NET 8 microservice to upload files and persist content + metadata in Postgres, mirroring sibling service conventions."
status: pending
priority: P2
effort: 12h
branch: dev
tags: [microservice, dotnet, file-upload, postgres, ef-core]
created: 2026-07-21
---

# FileService Implementation Plan

New microservice `services/FileService/` that accepts file uploads (multipart/form-data), validates them, and stores **binary content + metadata in Postgres** (per user request "lưu file vào DB"). Mirrors the CQRS/MediatR + EF Core + Keycloak conventions of sibling services (PatientService as reference template).

## Prior Art (codebase findings — reuse, don't re-derive)
- `ImagingService.ImagingResult` stores `ImageUrl` string (a reference), NOT blobs — so no existing in-DB file storage to duplicate. FileService is greenfield.
- Auth = **Keycloak** via `HospitalShared.Auth` (`AddKeycloakAuth`, `[Authorize(Roles = Roles.X)]`), NOT plain JWT from AuthServiceDotnet.
- IDs = `HospitalShared.GuidV7.NewGuid()`. Entities = private setters + static `Create` factory.
- Schema created at startup with `db.Database.EnsureCreatedAsync()` — **no EF migrations in repo**. Follow this (no migration files).
- Standard package set + `HospitalShared` project reference (see PatientService.csproj).
- Infra folders convention: `Infrastructure/{Persistence,Repositories,MessageBus,HttpClients}`.
- Serilog (Console+Seq+Loki+Http), Prometheus (`MapMetrics`/`UseHttpMetrics`), Swagger (Dev only), `ExceptionHandlingMiddleware`, `/health` controller.
- Port allocation: 5001 Patient … 5012 Imaging used → **FileService = 5013**.
- Orchestrator wires downstream via typed HttpClients keyed on `Services:XxxService` config (not a generic proxy).

## Phases
| # | Phase | File | Status | Effort |
|---|-------|------|--------|--------|
| 1 | Scaffold project (csproj, Program.cs, Dockerfiles, appsettings, health) | [phase-01-scaffold-project.md](phase-01-scaffold-project.md) | pending | 2h |
| 2 | Domain entity + persistence (StoredFile, config, DbContext, repository) | [phase-02-domain-persistence.md](phase-02-domain-persistence.md) | pending | 2.5h |
| 3 | Application layer (upload/get/list/delete commands, handlers, DTOs, validation service) | [phase-03-application-layer.md](phase-03-application-layer.md) | pending | 3h |
| 4 | API controller (upload/download/list/delete endpoints, auth, streaming) | [phase-04-api-controller.md](phase-04-api-controller.md) | pending | 2h |
| 5 | Deployment (docker-compose entry, optional Orchestrator route, docs) | [phase-05-infra-deployment.md](phase-05-infra-deployment.md) | pending | 1h |
| 6 | Testing & review | [phase-06-testing-review.md](phase-06-testing-review.md) | pending | 1.5h |

## Key Dependencies
- `shared/HospitalShared` (Auth, GuidV7, Metrics, Tracing, Outbox) — referenced, no changes needed.
- Postgres `hospital_db` (shared DB, existing connection string).
- Keycloak realm (existing) for auth.

## Storage Decision (DEFAULT: in-DB `bytea`)
User explicitly asked to store files in the DB. Plan defaults to a `bytea` content column with a hard size cap (default 20 MB) to protect Postgres/memory. Structured so switching to disk/object-storage later needs only the repository + entity `Content` field to change (metadata table stays). See Unresolved Questions.

## Unresolved Questions
1. **Storage medium**: confirm in-DB `bytea` (planned default) vs. disk/object-storage w/ metadata-only. In-DB is simplest (KISS) but bloats Postgres & backups for large/many files. Recommend keeping cap low; revisit if volume grows.
2. **Max file size & allowed content types**: plan assumes 20 MB cap + allowlist (pdf, png, jpg, jpeg, gif, webp, docx, xlsx, txt, csv). Confirm business needs.
3. **Auth scope**: which Keycloak roles may upload/download/delete? Plan defaults upload/list/delete to `Roles.AdminDoctorReceptionist`, download to `Roles.AdminDoctorReceptionistPatient`. Confirm.
4. **Orchestrator routing**: is exposing FileService through OrchestratorService/BFF in scope now? Plan treats it as optional (phase 5), off by default.
5. **Ownership/soft-delete**: reuse `IsActive` soft-delete pattern (like Patient) or hard delete? Plan assumes soft-delete.
6. **Kafka/Outbox events**: emit `FileUploaded`/`FileDeleted` events? Plan omits by default (YAGNI) — add only if a consumer exists.
