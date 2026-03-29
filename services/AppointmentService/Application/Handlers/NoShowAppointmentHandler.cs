using AppointmentService.Application.Commands;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Marks appointment as no-show. No refund — patient didn't attend.</summary>
public class NoShowAppointmentHandler : IRequestHandler<NoShowAppointmentCommand>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly ILogger<NoShowAppointmentHandler> _logger;

    public NoShowAppointmentHandler(IAppointmentRepository appointmentRepo, ILogger<NoShowAppointmentHandler> logger)
    {
        _appointmentRepo = appointmentRepo;
        _logger = logger;
    }

    public async Task Handle(NoShowAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(cmd.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", cmd.AppointmentId);

        appointment.MarkNoShow();
        await _appointmentRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Appointment {AppointmentId} marked as no-show", cmd.AppointmentId);
    }
}
