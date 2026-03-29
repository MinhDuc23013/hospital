using MediatR;

namespace AppointmentService.Application.Commands;

/// <summary>Mark appointment as no-show (patient didn't attend). Doctor/admin action.</summary>
public record NoShowAppointmentCommand(Guid AppointmentId) : IRequest;
