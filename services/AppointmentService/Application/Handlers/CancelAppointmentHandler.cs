using AppointmentService.Application.Commands;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.HttpClients;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Cancels appointment + refunds payment (if paid) + releases slot + updates saga.</summary>
public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, CancelAppointmentResult>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IBookingSagaRepository _sagaRepo;
    private readonly IBookingSagaLogRepository _logRepo;
    private readonly DoctorScheduleServiceClient _scheduleClient;
    private readonly PaymentServiceClient _paymentClient;
    private readonly ILogger<CancelAppointmentHandler> _logger;

    public CancelAppointmentHandler(
        IAppointmentRepository appointmentRepo,
        IBookingSagaRepository sagaRepo,
        IBookingSagaLogRepository logRepo,
        DoctorScheduleServiceClient scheduleClient,
        PaymentServiceClient paymentClient,
        ILogger<CancelAppointmentHandler> logger)
    {
        _appointmentRepo = appointmentRepo;
        _sagaRepo = sagaRepo;
        _logRepo = logRepo;
        _scheduleClient = scheduleClient;
        _paymentClient = paymentClient;
        _logger = logger;
    }

    public async Task<CancelAppointmentResult> Handle(CancelAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(cmd.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", cmd.AppointmentId);

        var saga = await _sagaRepo.GetActiveByAppointmentIdAsync(cmd.AppointmentId, ct);

        if (appointment.Status == AppointmentStatus.Cancelled && saga is null)
            return new CancelAppointmentResult(true, "Appointment already cancelled", false, false);

        if (appointment.Status != AppointmentStatus.Cancelled)
        {
            appointment.Cancel();
            await _appointmentRepo.SaveChangesAsync(ct);
        }
        if (saga is null)
        {
            _logger.LogWarning("No active saga found for appointment {AppointmentId}, skipping compensation", cmd.AppointmentId);
            return new CancelAppointmentResult(true, "Appointment cancelled (no active saga found)", false, false);
        }

        var previousStep = saga.CurrentStep.ToString();
        saga.MarkCompensating();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga.Id, previousStep, "Compensating", "User cancelled appointment", ct);

        // Release slot
        var slotReleased = await _scheduleClient.ReleaseSlotAsync(saga.ScheduleId, saga.SlotId, ct);
        _logger.LogInformation("Slot {SlotId} release {Result} for cancelled appointment {AppointmentId}",
            saga.SlotId, slotReleased ? "succeeded" : "failed", cmd.AppointmentId);
        await LogStepAsync(saga.Id, "Compensating", "Compensating",
            slotReleased ? $"Released slot {saga.SlotId}" : $"Failed to release slot {saga.SlotId}", ct);

        // Cancel or refund payment depending on state
        var paymentHandled = false;
        if (saga.PaymentId.HasValue)
        {
            // Try cancel first (for Pending/Processing), fallback to refund (for Completed)
            paymentHandled = await _paymentClient.CancelPaymentAsync(saga.PaymentId.Value, ct);
            if (!paymentHandled)
                paymentHandled = await _paymentClient.RefundPaymentAsync(saga.PaymentId.Value, ct);

            var action = paymentHandled ? "Cancelled/Refunded" : "Failed to cancel/refund";
            _logger.LogInformation("Payment {PaymentId} {Action} for cancelled appointment {AppointmentId}",
                saga.PaymentId, action, cmd.AppointmentId);
            await LogStepAsync(saga.Id, "Compensating", "Compensating",
                $"{action} payment {saga.PaymentId}", ct);
        }

        saga.MarkCompensated();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga.Id, "Compensating", "Compensated", "Cancellation complete", ct);

        return new CancelAppointmentResult(true, "Appointment cancelled successfully", slotReleased, paymentHandled);
    }

    private async Task LogStepAsync(Guid sagaId, string from, string to, string message, CancellationToken ct)
    {
        var log = BookingSagaLog.Create(sagaId, from, to, message);
        await _logRepo.AddAsync(log, ct);
        await _logRepo.SaveChangesAsync(ct);
    }
}
