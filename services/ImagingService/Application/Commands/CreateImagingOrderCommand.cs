using ImagingService.Application.DTOs;
using ImagingService.Domain.Enums;
using MediatR;

namespace ImagingService.Application.Commands;

public record CreateImagingOrderCommand(
    Guid PatientId,
    Guid AppointmentId,
    string DoctorId,
    ImagingType Type,
    string BodyPart,
    string? ClinicalHistory,
    decimal Price = 0
) : IRequest<ImagingOrderDto>;
