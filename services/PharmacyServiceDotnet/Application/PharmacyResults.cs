using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Application;

/// <summary>Read model returned by drug query/command handlers.</summary>
public record DrugResult(
    Guid Id,
    string Name,
    string Code,
    string? Dosage,
    int Quantity,
    decimal Price,
    int LowStockThreshold,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Single prescription item within a prescription.</summary>
public record PrescriptionItem(
    Guid DrugId,
    string DrugName,
    int Quantity,
    string? Dosage
);

/// <summary>Read model returned by prescription query/command handlers.</summary>
public record PrescriptionResult(
    Guid Id,
    Guid PatientId,
    string DoctorId,
    Guid? AppointmentId,
    string Status,
    string? Notes,
    List<PrescriptionItem> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
