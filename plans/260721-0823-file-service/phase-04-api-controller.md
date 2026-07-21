# Phase 04 — API Controller

## Context Links
- Overview: [plan.md](plan.md); depends on [phase-03-application-layer.md](phase-03-application-layer.md)
- Template: `PatientService/Controllers/PatientsController.cs`

## Overview
- Priority: P1
- Status: pending
- `FilesController` exposing upload/download/list/delete over `api/files`, secured with Keycloak roles, streaming download.

## Key Insights
- Upload endpoint accepts `IFormFile` via `[FromForm]` multipart; controller extracts `uploadedBy` from `User` claims (Keycloak subject) — NOT from request body.
- Download returns `FileContentResult`/`File(...)` with correct content-type + `Content-Disposition` (sanitized filename).
- Reuse `Roles` constants from `HospitalShared.Auth`.

## Requirements
- `POST api/files` (multipart) → 201 + `FileMetadataDto`. `[Authorize(Roles = Roles.AdminDoctorReceptionist)]` (confirm Q3).
- `GET api/files/{id}` → file stream (download). `[Authorize(Roles = Roles.AdminDoctorReceptionistPatient)]`.
- `GET api/files/{id}/metadata` → `FileMetadataDto` (no bytes).
- `GET api/files?page=&pageSize=` → `{ data, pagination }` (mirror Patient list shape).
- `DELETE api/files/{id}` → soft-delete `{ success, message }`.

## Architecture
- Thin controller → `IMediator.Send`. `RequestSizeLimit`/`DisableRequestSizeLimit` handled via Kestrel/Form limits from phase 1 (config-driven).

## Related Code Files
Create:
- `services/FileService/Controllers/FilesController.cs`

## Implementation Steps
1. `FilesController : ControllerBase`, `[ApiController]`, `[Route("api/[controller]")]`.
2. Upload: `[HttpPost]` `[RequestFormLimits]`; accept `IFormFile file`; guard null/empty; copy stream to bytes; build `UploadFileCommand` with `uploadedBy = User.FindFirst("sub")?.Value ?? User.Identity?.Name`; `CreatedAtAction(nameof(GetMetadata), ...)`.
3. Download: `[HttpGet("{id:guid}")]`; get `FileDownloadDto`; `NotFound()` if null; `return File(dto.Content, dto.ContentType, dto.FileName)`.
4. Metadata: `[HttpGet("{id:guid}/metadata")]`.
5. List: `[HttpGet]` page/pageSize.
6. Delete: `[HttpDelete("{id:guid}")]`.
7. Apply `[Authorize(Roles = ...)]` per endpoint. Build.

## Todo List
- [ ] FilesController with 5 endpoints
- [ ] uploadedBy from Keycloak claims
- [ ] Streaming download w/ Content-Disposition
- [ ] Per-endpoint role authorization
- [ ] build passes

## Success Criteria
- Endpoints compile; upload persists & returns 201; download returns original bytes+name; list excludes blobs; unauthorized roles get 403.

## Risk Assessment
- Missing size limit attribute → 413/large-body abuse. Mitigate: config-driven limits + `[RequestFormLimits]`.
- Content-Disposition header injection via filename. Mitigate: sanitized filename (phase 3).

## Security Considerations
- All endpoints `[Authorize]`; download role broader than upload only if Q3 confirms; consider owner check if patients may only fetch own files (flag).

## Next Steps
- Phase 05 deployment + optional Orchestrator routing.
