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

/// <summary>Read model for a drug batch/lot.</summary>
public record DrugBatchResult(
    Guid Id,
    Guid DrugId,
    string BatchNumber,
    DateTime ExpiryDate,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTime ReceivedDate,
    DateTime CreatedAt
);

/// <summary>Read model for dispensing saga state.</summary>
public record DispensingSagaResult(
    Guid Id,
    Guid PrescriptionId,
    Guid PatientId,
    string DoctorId,
    string CurrentStep,
    Guid? PaymentId,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
