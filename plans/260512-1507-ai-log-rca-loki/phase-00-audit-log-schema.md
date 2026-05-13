---
phase: 0
status: pending
priority: critical
effort: 1 day
---

# Phase 0: Audit Log Schema cho PHI

## Context Links

- Plan: [plan.md](plan.md)
- Brainstorm: [`brainstorm-260512-1507-ai-log-rca-loki.md`](../reports/brainstorm-260512-1507-ai-log-rca-loki.md) §5 PII Redaction

## Overview

- **Priority**: Critical — gate cho mọi phase sau
- **Status**: pending
- **Description**: Audit toàn bộ 16 services xem structured log key nào chứa PII/PHI. Output là spec input cho PII redactor.

## Key Insights

- Hệ thống có 16 services với Serilog. Mỗi service log structured (key-value) qua Seq/Loki/ELK.
- Không biết hiện trạng cụ thể: service nào log `patientName`, `phoneNumber`, `diagnosis`, etc.
- Không redact đúng → leak ra Anthropic → vi phạm dù MVP "no BAA".

## Requirements

**Functional**:
- List ra tất cả Serilog log calls có structured property tên đáng nghi (patient*, *Name, *Phone, *Email, diagnosis, etc.)
- Phân loại 3 mức: PHI direct (tên, sđt, BHYT), PHI indirect (patient ID, encounter ID), safe (correlation ID, traceId).
- Output: 1 file markdown spec `docs/logging-phi-audit.md` liệt kê field theo service.

**Non-functional**: Không sửa code, chỉ audit.

## Architecture

Không có architecture — phase này là discovery.

## Related Code Files

**Đọc** (read-only):
- `services/*/Program.cs` — Serilog config
- `services/*/Controllers/*.cs` — `_logger.LogXxx(...)` calls
- `services/*/Application/Handlers/*.cs` — handler logging
- `services/*/Infrastructure/Messaging/*.cs` — event consumer logging
- `shared/HospitalShared/**/*.cs` — shared logging

**Tạo mới**:
- `docs/logging-phi-audit.md`

## Implementation Steps

1. Grep pattern `_logger\.(LogInformation|LogWarning|LogError|LogDebug)` trên toàn bộ `services/` và `shared/`.
2. Filter calls có structured property suspicious: regex `\{[A-Z][a-zA-Z]*\}` trong message template (vd `"Created patient {PatientName}"`).
3. Cross-reference với DTOs trong `shared/HospitalShared/DTOs/*.cs` — field nào của DTO bị log trực tiếp.
4. Classify từng key:
   - **PHI-direct**: patientName, fullName, dateOfBirth, address, phone, email, bhyt, cccd, diagnosis
   - **PHI-indirect**: patientId, encounterId, appointmentId, medicalRecordId
   - **Safe**: traceId, spanId, correlationId, userId (staff), serviceName
5. Viết spec markdown:
   ```markdown
   # Logging PHI Audit
   ## PatientService
   - PHI-direct: PatientName (Patients.cs:42), Phone (Handlers/CreatePatient.cs:78)
   - PHI-indirect: PatientId
   ## AppointmentService
   ...
   ```
6. Identify "hot files" — file log PHI direct nhiều nhất → cảnh báo team không log thêm.
7. Output regex pack đề xuất cho redactor (Phase 1):
   - Structured keys cần mask: `["patientName", "fullName", "phone", "phoneNumber", "email", "bhyt", "cccd", "address", "diagnosis"]`
   - Value patterns: CCCD `\b\d{9}\b|\b\d{12}\b`, phone VN `(0|\+84)\d{9,10}`, BHYT `[A-Z]{2}\d{13}`, email RFC

## Todo List

- [ ] Grep `_logger.Log*` toàn project, save raw results
- [ ] Filter suspicious structured properties
- [ ] Cross-reference với DTOs
- [ ] Classify mỗi key vào 3 mức (direct/indirect/safe)
- [ ] Viết `docs/logging-phi-audit.md`
- [ ] Đề xuất regex pack + key blocklist cho Phase 1
- [ ] Review với 1 dev khác để xác nhận classification

## Success Criteria

- File `docs/logging-phi-audit.md` tồn tại, list đầy đủ 16 services
- Có blocklist keys cụ thể cho Phase 1 implement
- Có regex value patterns cụ thể cho Phase 1 implement

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Miss 1 field PHI → leak | Peer review audit, prefer over-redact |
| Audit lỗi thời sau code thay đổi | Tag file `logging-phi-audit.md` cần update khi đổi log schema (CI lint optional) |

## Security Considerations

- Audit output (markdown spec) KHÔNG chứa value PHI thật — chỉ tên field.

## Next Steps

- Output → input direct cho Phase 1 PiiRedactor implementation.
