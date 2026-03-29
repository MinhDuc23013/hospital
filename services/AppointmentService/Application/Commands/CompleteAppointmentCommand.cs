using MediatR;

namespace AppointmentService.Application.Commands;

/// <summary>Mark appointment as completed after consultation (doctor action).</summary>
public record CompleteAppointmentCommand(Guid AppointmentId) : IRequest;
