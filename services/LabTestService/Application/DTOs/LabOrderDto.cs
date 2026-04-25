using LabTestService.Domain.Enums;

namespace LabTestService.Application.DTOs;

public record LabOrderItemDto(
    Guid Id,
    string TestName,
    string TestCode,
    string Category,
    decimal UnitPrice,
    string? Result,
    string? NormalRange,
    string? Unit,
    LabResultStatus ResultStatus,
    DateTime? ResultedAt);

public record LabOrderDto(
    Guid Id,
    Guid PatientId,
    Guid AppointmentId,
    string DoctorId,
    LabOrderStatus Status,
    string? Notes,
    DateTime OrderedAt,
    DateTime UpdatedAt,
    IReadOnlyList<LabOrderItemDto> Items);
