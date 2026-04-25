using ImagingService.Domain.Enums;

namespace ImagingService.Application.DTOs;

public record ImagingResultDto(
    Guid Id,
    string Findings,
    string Impression,
    string? ImageUrl,
    string ReportedBy,
    DateTime ReportedAt);

public record ImagingOrderDto(
    Guid Id,
    Guid PatientId,
    Guid AppointmentId,
    string DoctorId,
    ImagingType Type,
    string BodyPart,
    string? ClinicalHistory,
    decimal Price,
    ImagingOrderStatus Status,
    DateTime OrderedAt,
    DateTime UpdatedAt,
    ImagingResultDto? Result);
