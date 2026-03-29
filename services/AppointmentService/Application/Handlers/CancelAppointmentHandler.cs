using AppointmentService.Application.Commands;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.HttpClients;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Cancels appointment + refunds payment (if paid) + releases slot.</summary>
public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IBookingSagaRepository _sagaRepo;
    private readonly DoctorScheduleServiceClient _scheduleClient;
    private readonly PaymentServiceClient _paymentClient;
    private readonly ILogger<CancelAppointmentHandler> _logger;

    public CancelAppointmentHandler(
        IAppointmentRepository appointmentRepo,
        IBookingSagaRepository sagaRepo,
        DoctorScheduleServiceClient scheduleClient,
        PaymentServiceClient paymentClient,
        ILogger<CancelAppointmentHandler> logger)
    {
        _appointmentRepo = appointmentRepo;
        _sagaRepo = sagaRepo;
        _scheduleClient = scheduleClient;
        _paymentClient = paymentClient;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(cmd.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", cmd.AppointmentId);

        if (appointment.Status == AppointmentStatus.Cancelled)
            return true;

        // Cancel the appointment
        appointment.Cancel();
        await _appointmentRepo.SaveChangesAsync(ct);

        // Find saga to get slot + payment info for compensation
        var sagas = await _sagaRepo.GetByAppointmentIdAsync(cmd.AppointmentId, ct);
        if (sagas is null)
        {
            _logger.LogWarning("No saga found for appointment {AppointmentId}, skipping compensation", cmd.AppointmentId);
            return true;
        }

        // Release slot
        await _scheduleClient.ReleaseSlotAsync(sagas.ScheduleId, sagas.SlotId, ct);
        _logger.LogInformation("Released slot {SlotId} for cancelled appointment {AppointmentId}", sagas.SlotId, cmd.AppointmentId);

        // Refund payment if exists
        if (sagas.PaymentId.HasValue)
        {
            await _paymentClient.RefundPaymentAsync(sagas.PaymentId.Value, ct);
            _logger.LogInformation("Refunded payment {PaymentId} for cancelled appointment {AppointmentId}", sagas.PaymentId, cmd.AppointmentId);
        }

        return true;
    }
}
