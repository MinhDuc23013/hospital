# Phase 01 — Scaffold Project

## Context Links
- Overview: [plan.md](plan.md)
- Template: `services/PatientService/` (csproj, Program.cs, Dockerfile, Dockerfile.local, appsettings.json, Controllers/HealthController.cs)

## Overview
- Priority: P1 (blocks all later phases)
- Status: pending
- Create `services/FileService/` skeleton matching sibling-service scaffolding + wire Program.cs pipeline. Port **5013**.

## Key Insights
- Follow PatientService Program.cs almost 1:1: Serilog, EF+Npgsql w/ retry + `AddMetricsInterceptor`, MediatR, FluentValidation, Prometheus, Jaeger tracing, Keycloak auth, Swagger (Dev only), `ExceptionHandlingMiddleware`, `MapMetrics`.
- Schema via `EnsureCreatedAsync()` — no migrations.
- Kafka/Outbox/NotificationPublisher are OPTIONAL for FileService (YAGNI) — omit unless events needed (see plan Q6). Keep Program.cs lean.

## Requirements
- Compilable .NET 8 web project referencing `..\..\shared\HospitalShared\HospitalShared.csproj`.
- Endpoints listen on `http://+:5013`; `/health` returns healthy JSON.
- Multipart request limits raised for uploads (`FormOptions.MultipartBodyLengthLimit`, Kestrel `MaxRequestBodySize`) to configured cap.

## Architecture
Standard layered layout: `Application/`, `Controllers/`, `Domain/`, `Infrastructure/`, `Middleware/`, `Properties/`.

## Related Code Files
Create:
- `services/FileService/FileService.csproj` (copy PatientService package set; drop Confluent.Kafka/RabbitMQ if not emitting events)
- `services/FileService/Program.cs`
- `services/FileService/appsettings.json` (ConnectionStrings:PostgreSQL → hospital_db; add `FileStorage:MaxSizeBytes`, `FileStorage:AllowedContentTypes`; `Urls: http://+:5013`)
- `services/FileService/Dockerfile` (EXPOSE 5013, ASPNETCORE_URLS 5013)
- `services/FileService/Dockerfile.local`
- `services/FileService/.dockerignore`
- `services/FileService/Controllers/HealthController.cs` (service = "FileService")
- `services/FileService/Middleware/ExceptionHandlingMiddleware.cs` (copy from PatientService)
- `services/FileService/Properties/launchSettings.json`

## Implementation Steps
1. Copy PatientService.csproj → FileService.csproj; set `RootNamespace`/assembly to `FileService`; keep EF+Npgsql, MediatR, FluentValidation, Serilog set, Swashbuckle, prometheus-net; remove Kafka/RabbitMQ packages (add back only if Q6=yes).
2. Author Program.cs: Serilog block (label `service=file-service`), `AddDbContext<FileDbContext>` w/ retry + metrics interceptor, MediatR + validators from assembly, Jaeger tracing "file-service", `AddKeycloakAuth`, controllers, Swagger (Dev). Configure form/body size limits from `FileStorage:MaxSizeBytes`.
3. `EnsureCreatedAsync()` on startup scope.
4. appsettings.json: dev connection string (reuse Patient's), `FileStorage` section, `Urls`.
5. Dockerfile + Dockerfile.local + .dockerignore adapted (paths → FileService, port 5013).
6. HealthController + ExceptionHandlingMiddleware + launchSettings.
7. `dotnet build services/FileService/FileService.csproj` — must compile clean.

## Todo List
- [ ] csproj created & restores
- [ ] Program.cs pipeline wired
- [ ] appsettings + FileStorage config
- [ ] Dockerfile(s) + .dockerignore
- [ ] HealthController + middleware + launchSettings
- [ ] `dotnet build` passes

## Success Criteria
- Project builds; app boots; `GET /health` → 200; Swagger loads in Dev; no compile errors.

## Risk Assessment
- Wrong port collision → verify 5013 unused. Mitigate: grep Dockerfiles (done: free).
- Body-size limits not raised → large uploads 413. Mitigate: set Kestrel + Form limits from config.

## Security Considerations
- Auth middleware ordering (`UseAuthentication` before `UseAuthorization`). Controllers get `[Authorize]` in later phases.

## Next Steps
- Phase 02 (domain + persistence) depends on this scaffold.
