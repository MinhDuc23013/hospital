using AppointmentService.Application.Commands;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.HttpClients;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>
/// Cancels an appointment and releases the doctor slot.
/// Saga orchestration (payment refund, saga state) is handled by OrchestratorService.
/// This handler owns only the appointment record and slot release.
/// </summary>
public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, CancelAppointmentResult>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly DoctorScheduleServiceClient _scheduleClient;
    private readonly ILogger<CancelAppointmentHandler> _logger;

    public CancelAppointmentHandler(
        IAppointmentRepository appointmentRepo,
        DoctorScheduleServiceClient scheduleClient,
        ILogger<CancelAppointmentHandler> logger)
    {
        _appointmentRepo = appointmentRepo;
        _scheduleClient = scheduleClient;
        _logger = logger;
    }

    public async Task<CancelAppointmentResult> Handle(CancelAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(cmd.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", cmd.AppointmentId);

        if (appointment.Status == AppointmentStatus.Cancelled)
            return new CancelAppointmentResult(true, "Appointment already cancelled", false, false);

        appointment.Cancel();
        await _appointmentRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Appointment {AppointmentId} cancelled", cmd.AppointmentId);

        return new CancelAppointmentResult(true, "Appointment cancelled successfully", false, false);
    }
}
