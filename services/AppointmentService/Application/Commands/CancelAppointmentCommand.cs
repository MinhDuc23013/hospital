using MediatR;

namespace AppointmentService.Application.Commands;

public record CancelAppointmentCommand(Guid AppointmentId) : IRequest<CancelAppointmentResult>;

public record CancelAppointmentResult(
    bool Success,
    string Message,
    bool SlotReleased,
    bool PaymentRefunded
);
