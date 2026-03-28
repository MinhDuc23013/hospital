using MediatR;

namespace AppointmentService.Application.Commands;

public record CancelAppointmentCommand(Guid AppointmentId) : IRequest<bool>;
