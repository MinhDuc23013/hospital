using LabTestService.Application.DTOs;
using MediatR;

namespace LabTestService.Application.Commands;

public record LabOrderItemInput(string TestName, string TestCode, string Category, decimal UnitPrice = 0);

public record CreateLabOrderCommand(
    Guid PatientId,
    Guid AppointmentId,
    string DoctorId,
    string? Notes,
    List<LabOrderItemInput> Items
) : IRequest<LabOrderDto>;
