# Logging PHI Audit

**Date**: 2026-05-12
**Purpose**: Input spec cho AiRcaService PII Redactor (Phase 1)
**Status**: Complete — update khi thay đổi log schema

---

## Risk Summary

| Service | Risk | PHI Fields Logged |
|---------|------|-------------------|
| NotificationService | **CRITICAL** | `{To}` (email), `{Phone}` |
| AuthService | **HIGH** | `{Email}` |
| PatientService | **HIGH** | `{Email}`, PatientDto.PhoneNumber/FirstName/LastName/DateOfBirth |
| DoctorScheduleService | **MEDIUM** | `{Email}` |
| MedicalRecordService | **MEDIUM** | Implicit via PatientId (no direct PHI name) |
| Other services | **LOW** | PHI-indirect IDs only |

---

## Detailed Findings

### NotificationService — CRITICAL

```
SmtpEmailService.cs:26   LogWarning("SMTP not configured — email to {To} logged only...")
SmtpEmailService.cs:51   LogInformation("Email sent to {To}, subject: {Subject}")
SmtpEmailService.cs:56   LogError("Failed to send email to {To}")
SendEmailHandler.cs:20   LogInformation("Sending email to {To}, subject: {Subject}")
HttpSmsService.cs:23     LogWarning("SMS provider not configured — SMS to {Phone} logged only...")
HttpSmsService.cs:29     LogInformation("SMS sent to {Phone}: {Message}")
SendSmsHandler.cs:20     LogInformation("Sending SMS to {Phone}")
```

PHI keys: `To`, `Phone`, `Subject`, `Message` (SMS context)

### AuthService — HIGH

```
UsersController.cs:38    LogWarning("SEED: First admin account created: {Email}")
UsersController.cs:54    LogInformation("User created: {Email} role={Role}")
KeycloakAdminClient.cs:83 LogInformation("Created Keycloak user {Email} with ID {UserId}")
```

PHI keys: `Email`

### PatientService — HIGH

```
CreatePatientHandler.cs:42  LogInformation("Keycloak user created for patient {Email}: {KeycloakUserId}")
```

PHI keys: `Email`
DTO PHI fields (PatientDto): `FirstName`, `LastName`, `DateOfBirth`, `PhoneNumber`, `Email`

### DoctorScheduleService — MEDIUM

```
CreateDoctorHandler.cs:36   LogInformation("Keycloak user created for doctor {Email}: {KeycloakUserId}")
```

PHI keys: `Email`

### MedicalRecordService — MEDIUM

```
CreateMedicalRecordHandler.cs:33  LogInformation("Medical record {RecordId} created for patient {PatientId}")
```

PHI-indirect only. No direct PII in message. OK.

---

## PHI Classification

### PHI-direct (block từ log hoặc redact trước khi gửi LLM)
```
Keys: Email, To, Phone, PhoneNumber, FirstName, LastName, FullName,
      DateOfBirth, Subject (email context), Message (SMS context),
      Diagnosis, Findings
```

### PHI-indirect (IDs — cho phép log, không redact key, nhưng coi là sensitive)
```
PatientId, EncounterId, AppointmentId, MedicalRecordId, PrescriptionId
```

### Safe (log tự do)
```
TraceId, SpanId, CorrelationId, ServiceName, UserId (staff), Role, SlotId, DoctorId
```

---

## Recommended Key Blocklist (cho AiRcaService PiiRedactor)

```csharp
public static readonly string[] BlockedKeys =
[
    "Email", "To", "Phone", "PhoneNumber",
    "FirstName", "LastName", "FullName",
    "DateOfBirth",
    "Subject",      // email subject có thể chứa tên BN
    "Message",      // SMS body
    "Diagnosis", "Findings"
];
```

## Recommended Value Regex Patterns

```csharp
["EMAIL"]    = new Regex(@"\b[\w.\-]+@[\w.\-]+\.\w{2,}\b", RegexOptions.Compiled),
["PHONE_VN"] = new Regex(@"\b(0|\+84)[1-9]\d{8,9}\b", RegexOptions.Compiled),
["CCCD_12"]  = new Regex(@"\b\d{12}\b", RegexOptions.Compiled),
["CCCD_9"]   = new Regex(@"\b\d{9}\b", RegexOptions.Compiled),
["BHYT"]     = new Regex(@"\b[A-Z]{2}\d{13}\b", RegexOptions.Compiled),
["DATE_ISO"] = new Regex(@"\b\d{4}-\d{2}-\d{2}\b", RegexOptions.Compiled),
```

> **Trade-off CCCD_9**: 9-digit match có false positive với order ID, port number, etc.
> → Prefer over-redact: chấp nhận false positive, không chấp nhận false negative với PHI.

---

## ⚠️ Phát hiện quan trọng

1. **Không có redaction layer hiện tại** — tất cả structured properties ghi thẳng ra Loki/Seq/ELK.
2. **NotificationService critical nhất**: log email + phone + SMS content trước khi gửi → nếu send fail, PHI tồn tại mãi trong log.
3. **{Subject} email** có thể chứa tên bệnh nhân (vd "Appointment reminder for Nguyen Van A").

---

## Maintenance

Update file này khi:
- Thêm `_logger.Log*` call mới với structured properties
- Thêm field mới vào Patient/Medical DTOs
- Team có yêu cầu audit thêm field nào
