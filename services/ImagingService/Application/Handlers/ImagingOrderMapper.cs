using ImagingService.Application.DTOs;
using ImagingService.Domain.Entities;

namespace ImagingService.Application.Handlers;

/// <summary>Maps ImagingOrder domain entity to DTO — keeps mapping logic in one place.</summary>
internal static class ImagingOrderMapper
{
    internal static ImagingOrderDto MapToDto(ImagingOrder o) => new(
        o.Id,
        o.PatientId,
        o.AppointmentId,
        o.DoctorId,
        o.Type,
        o.BodyPart,
        o.ClinicalHistory,
        o.Price,
        o.Status,
        o.OrderedAt,
        o.UpdatedAt,
        o.Result is null
            ? null
            : new ImagingResultDto(
                o.Result.Id,
                o.Result.Findings,
                o.Result.Impression,
                o.Result.ImageUrl,
                o.Result.ReportedBy,
                o.Result.ReportedAt));
}
