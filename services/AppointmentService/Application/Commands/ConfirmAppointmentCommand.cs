using MediatR;

namespace AppointmentService.Application.Commands;

/// <summary>Confirm an appointment after the async booking saga completes.</summary>
public record ConfirmAppointmentCommand(Guid AppointmentId) : IRequest;
