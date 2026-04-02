namespace MedicalRecordServiceDotnet.Application;

public record MedicalRecordResult(
    string Id,
    string PatientId,
    string AppointmentId,
    string Findings,
    List<string> Diagnosis,
    List<LabResultDto> LabResults,
    string? CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record LabResultDto(
    string TestName,
    string? Result,
    string? NormalRange,
    DateTime Timestamp
);

public record PaginatedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);
