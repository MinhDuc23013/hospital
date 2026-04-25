using LabTestService.Application.DTOs;
using LabTestService.Domain.Entities;

namespace LabTestService.Application.Handlers;

/// <summary>Static mapper — converts LabOrder aggregate to LabOrderDto. Shared by all handlers.</summary>
internal static class LabOrderMapper
{
    internal static LabOrderDto MapToDto(LabOrder order) => new(
        order.Id,
        order.PatientId,
        order.AppointmentId,
        order.DoctorId,
        order.Status,
        order.Notes,
        order.OrderedAt,
        order.UpdatedAt,
        order.Items.Select(i => new LabOrderItemDto(
            i.Id,
            i.TestName,
            i.TestCode,
            i.Category,
            i.UnitPrice,
            i.Result,
            i.NormalRange,
            i.Unit,
            i.ResultStatus,
            i.ResultedAt)).ToList().AsReadOnly());
}
