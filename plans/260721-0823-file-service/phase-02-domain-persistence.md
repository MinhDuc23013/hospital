# Phase 02 — Domain Entity & Persistence

## Context Links
- Overview: [plan.md](plan.md); depends on [phase-01-scaffold-project.md](phase-01-scaffold-project.md)
- Templates: `PatientService/Domain/Entities/Patient.cs`, `Infrastructure/Persistence/PatientConfiguration.cs`, `PatientDbContext.cs`, `Infrastructure/Repositories/PatientRepository.cs`

## Overview
- Priority: P1
- Status: pending
- Define `StoredFile` aggregate, EF config (table `stored_files`), `FileDbContext`, and repository. In-DB `bytea` content column (see plan storage decision).

## Key Insights
- Entity style: private setters + static `Create(...)` factory using `GuidV7.NewGuid()`; soft-delete via `IsActive` + `Deactivate()` (mirror Patient).
- Keep `Content` (`byte[]`) mapped to `bytea`; large-object streaming not needed at 20 MB cap.
- Consider a computed `Sha256` hash column for dedup/integrity (optional, KISS — include, cheap).

## Requirements
- Table columns: `Id` (PK, uuid), `FileName` (original, required ≤255), `ContentType` (required ≤128), `SizeBytes` (bigint), `Content` (bytea, required), `Sha256` (char(64), nullable), `UploadedBy` (Keycloak user id/subject ≤100), `UploadedAt` (timestamptz default NOW()), `IsActive` (bool default true).
- Repository methods: `AddAsync`, `GetByIdAsync` (metadata+content), `GetMetadataByIdAsync` (projection, no bytes — for list/head), `ListAsync(page,pageSize)`, `SoftDeleteAsync`, `SaveChangesAsync`.

## Architecture
- `FileDbContext` exposes `DbSet<StoredFile> Files`; `OnModelCreating` applies `StoredFileConfiguration`. (Add `EventOutbox` only if Q6=yes.)
- Repository returns lightweight metadata projections for list to avoid loading blobs into memory.

## Related Code Files
Create:
- `services/FileService/Domain/Entities/StoredFile.cs`
- `services/FileService/Infrastructure/Persistence/FileDbContext.cs`
- `services/FileService/Infrastructure/Persistence/StoredFileConfiguration.cs`
- `services/FileService/Infrastructure/Repositories/IFileRepository.cs`
- `services/FileService/Infrastructure/Repositories/FileRepository.cs`

## Implementation Steps
1. `StoredFile` entity: fields above; `Create(fileName, contentType, sizeBytes, content, uploadedBy, sha256)`; `Deactivate()`.
2. `StoredFileConfiguration`: `ToTable("stored_files")`, key, max lengths, `Content` column type `bytea` + required, `UploadedAt` default `NOW()`, `IsActive` default true, index on `UploadedBy` and `UploadedAt`.
3. `FileDbContext` with `DbSet<StoredFile>`.
4. `IFileRepository` + `FileRepository` (EF). List uses `.Select(...)` projection excluding `Content`.
5. Register `AddDbContext<FileDbContext>` (already in Program from phase 1) + `IFileRepository`.
6. Build.

## Todo List
- [ ] StoredFile entity w/ factory + soft-delete
- [ ] StoredFileConfiguration (bytea, indexes)
- [ ] FileDbContext
- [ ] IFileRepository + FileRepository (metadata projection for list)
- [ ] DI registration + build passes

## Success Criteria
- `EnsureCreatedAsync` creates `stored_files`; repository CRUD compiles; list query does not select `Content`.

## Risk Assessment
- Loading blobs on list → memory blowup. Mitigate: metadata projection.
- `bytea` param size limits. Mitigate: enforce cap before persist (phase 3).

## Security Considerations
- `UploadedBy` sourced from authenticated Keycloak subject (set in handler, not client input).

## Next Steps
- Phase 03 consumes repository via handlers.
