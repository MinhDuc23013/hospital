# Phase 03 — Application Layer

## Context Links
- Overview: [plan.md](plan.md); depends on [phase-02-domain-persistence.md](phase-02-domain-persistence.md)
- Templates: `PatientService/Application/{Commands,Handlers,Queries,Validators,DTOs}`

## Overview
- Priority: P1
- Status: pending
- CQRS commands/queries + handlers via MediatR, DTOs, and a dedicated `FileValidationService` (size + content-type + extension allowlist). Keep each file <200 LOC.

## Key Insights
- Validation split out into its own service (DRY, testable, reused by upload handler) — not inline in controller.
- Content-type validation must check BOTH declared content-type AND file extension against allowlist; optionally magic-byte sniff (YAGNI unless Q2 demands).
- DTOs must never expose raw `Content` for list/metadata responses.

## Requirements
- `UploadFileCommand(Stream/byte[] content, string fileName, string contentType, string uploadedBy)` → returns `FileMetadataDto`.
- `GetFileQuery(Guid id)` → returns `FileDownloadDto` (content + contentType + fileName) or null.
- `ListFilesQuery(page, pageSize)` → `(items: FileMetadataDto[], total)`.
- `DeleteFileCommand(Guid id)` → soft-delete.
- `FileValidationService.Validate(fileName, contentType, sizeBytes)` throws `DomainException` on violation.

## Architecture
- Handlers depend on `IFileRepository` + (upload) `FileValidationService`.
- Config-driven limits injected via `IOptions<FileStorageOptions>` bound to `FileStorage` appsettings section.
- SHA-256 computed in upload handler before persist.

## Related Code Files
Create:
- `services/FileService/Application/Commands/UploadFileCommand.cs`
- `services/FileService/Application/Commands/DeleteFileCommand.cs`
- `services/FileService/Application/Queries/GetFileQuery.cs`
- `services/FileService/Application/Queries/ListFilesQuery.cs`
- `services/FileService/Application/Handlers/UploadFileHandler.cs`
- `services/FileService/Application/Handlers/GetFileHandler.cs`
- `services/FileService/Application/Handlers/ListFilesHandler.cs`
- `services/FileService/Application/Handlers/DeleteFileHandler.cs`
- `services/FileService/Application/DTOs/FileMetadataDto.cs`
- `services/FileService/Application/DTOs/FileDownloadDto.cs`
- `services/FileService/Application/Services/FileValidationService.cs`
- `services/FileService/Application/Configuration/FileStorageOptions.cs`
- `services/FileService/Domain/Exceptions/DomainException.cs` (copy from PatientService)

## Implementation Steps
1. `FileStorageOptions` (MaxSizeBytes, AllowedContentTypes[], AllowedExtensions[]); bind in Program via `Configure<FileStorageOptions>`.
2. `FileValidationService`: reject if size>Max, content-type not in allowlist, or extension not in allowlist; sanitize filename (strip path separators — anti path-traversal). Throw `DomainException`.
3. Upload handler: validate → read bytes (respecting cap) → compute SHA-256 → `StoredFile.Create` → `AddAsync` → `SaveChangesAsync` → map to `FileMetadataDto`.
4. Get/List/Delete handlers via repository.
5. Register `FileValidationService` in DI (scoped).
6. Build.

## Todo List
- [ ] FileStorageOptions + binding
- [ ] FileValidationService (size/type/ext/filename sanitize)
- [ ] Upload/Get/List/Delete commands+queries+handlers
- [ ] FileMetadataDto + FileDownloadDto (no blob leak in metadata)
- [ ] DomainException copied
- [ ] build passes

## Success Criteria
- Handlers compile; validation rejects oversize/disallowed types; metadata DTO excludes content bytes.

## Risk Assessment
- Reading entire file into memory at cap × concurrency → memory pressure. Mitigate: low cap + streaming read guard.
- Filename with `../` → path traversal if ever written to disk. Mitigate: sanitize now regardless of storage medium.

## Security Considerations
- Validate before persist; sanitize filenames; never trust client content-type alone (pair with extension check).

## Next Steps
- Phase 04 wires HTTP endpoints to these handlers.
