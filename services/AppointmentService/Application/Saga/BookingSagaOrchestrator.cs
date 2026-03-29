using System.Text.Json;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.HttpClients;
using AppointmentService.Infrastructure.MessageBus;
using AppointmentService.Infrastructure.Repositories;
using HospitalShared.Events;

namespace AppointmentService.Application.Saga;

/// <summary>
/// Saga orchestrator for the appointment booking flow.
/// Coordinates: CreateAppointment → ReserveSlot → ProcessPayment → ConfirmSlot → Notify.
/// Handles compensation (rollback) on failure with retry + outbox fallback.
/// </summary>
public class BookingSagaOrchestrator
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IBookingSagaRepository _sagaRepo;
    private readonly IBookingSagaLogRepository _logRepo;
    private readonly ICompensationOutboxRepository _outboxRepo;
    private readonly PatientServiceClient _patientClient;
    private readonly DoctorScheduleServiceClient _scheduleClient;
    private readonly PaymentServiceClient _paymentClient;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly ILogger<BookingSagaOrchestrator> _logger;

    private const int MaxImmediateRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public BookingSagaOrchestrator(
        IAppointmentRepository appointmentRepo,
        IBookingSagaRepository sagaRepo,
        IBookingSagaLogRepository logRepo,
        ICompensationOutboxRepository outboxRepo,
        PatientServiceClient patientClient,
        DoctorScheduleServiceClient scheduleClient,
        PaymentServiceClient paymentClient,
        EventPublisher events,
        NotificationPublisher notifications,
        ILogger<BookingSagaOrchestrator> logger)
    {
        _appointmentRepo = appointmentRepo;
        _sagaRepo = sagaRepo;
        _logRepo = logRepo;
        _outboxRepo = outboxRepo;
        _patientClient = patientClient;
        _scheduleClient = scheduleClient;
        _paymentClient = paymentClient;
        _events = events;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<BookingSaga> ExecuteAsync(
        Guid patientId, string providerId,
        Guid scheduleId, Guid slotId,
        DateTime scheduledTime, int durationMinutes,
        decimal paymentAmount, string paymentMethod, string currency,
        string? notes, CancellationToken ct)
    {
        var saga = BookingSaga.Create(patientId, providerId, scheduleId, slotId, paymentAmount, paymentMethod, notes);
        await _sagaRepo.AddAsync(saga, ct);
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Saga {SagaId} started for patient {PatientId}", saga.Id, patientId);

        try
        {
            await StepCreateAppointment(saga, patientId, providerId, scheduledTime, durationMinutes, notes, ct);
            await StepReserveSlot(saga, ct);
            await StepCreatePayment(saga, paymentAmount, currency, paymentMethod, ct);
            await StepConfirmSlot(saga, ct);

            // Saga now waits for external payment confirmation (webhook/callback)
            saga.MarkAwaitingPayment();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "SlotConfirmed", "AwaitingPayment", "Booking confirmed, awaiting payment", ct: ct);

            _logger.LogInformation("Saga {SagaId} awaiting payment. Appointment {AppointmentId}, Payment {PaymentId}",
                saga.Id, saga.AppointmentId, saga.PaymentId);
        }
        catch (SagaStepException ex)
        {
            _logger.LogWarning(ex, "Saga {SagaId} failed at step {Step}: {Reason}", saga.Id, saga.CurrentStep, ex.Message);
            var failedFrom = saga.CurrentStep.ToString();
            saga.MarkFailed(ex.Message);
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, failedFrom, "Failed", ex.Message, ct: ct);

            await CompensateAsync(saga, ct);
        }

        return saga;
    }

    // ── Steps ─────────────────────────────────────────────────────────────

    private async Task StepCreateAppointment(
        BookingSaga saga, Guid patientId, string providerId,
        DateTime scheduledTime, int durationMinutes, string? notes, CancellationToken ct)
    {
        var patient = await _patientClient.GetPatientAsync(patientId, ct);
        if (patient is null)
            throw new SagaStepException($"Patient '{patientId}' not found or PatientService unavailable.");

        var appointment = Appointment.Create(patientId, providerId, scheduledTime, durationMinutes, notes);
        await _appointmentRepo.AddAsync(appointment, ct);
        await _appointmentRepo.SaveChangesAsync(ct);

        saga.MarkAppointmentCreated(appointment.Id);
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} step AppointmentCreated: Appointment {AppointmentId} for patient {PatientId}", saga.Id, appointment.Id, patientId);
        await LogStepAsync(saga, "Started", "AppointmentCreated", $"Appointment {appointment.Id} created", ct: ct);
    }

    private async Task StepReserveSlot(BookingSaga saga, CancellationToken ct)
    {
        var slot = await _scheduleClient.ReserveSlotAsync(saga.ScheduleId, saga.SlotId, saga.PatientId, ct);
        if (slot is null)
            throw new SagaStepException("Failed to reserve slot — DoctorScheduleService unavailable or slot taken.");

        saga.MarkSlotReserved();
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} step SlotReserved: Slot {SlotId} on schedule {ScheduleId}", saga.Id, saga.SlotId, saga.ScheduleId);
        await LogStepAsync(saga, "AppointmentCreated", "SlotReserved", $"Slot {saga.SlotId} reserved", ct: ct);
    }

    /// <summary>Creates a Pending payment — does NOT process it. Payment confirmation comes async via webhook.</summary>
    private async Task StepCreatePayment(
        BookingSaga saga, decimal amount, string currency, string method, CancellationToken ct)
    {
        var payment = await _paymentClient.CreatePaymentAsync(
            saga.AppointmentId!.Value, saga.PatientId, amount, currency, method,
            $"Appointment booking #{saga.AppointmentId}", ct);
        if (payment is null)
            throw new SagaStepException("Failed to create payment — PaymentService unavailable.");

        saga.MarkPaymentCreated(payment.Id);
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} step PaymentCreated: Payment {PaymentId} amount {Amount} (Pending)", saga.Id, payment.Id, amount);
        await LogStepAsync(saga, "SlotReserved", "PaymentCreated", $"Payment {payment.Id} created (Pending)", ct: ct);
    }

    private async Task StepConfirmSlot(BookingSaga saga, CancellationToken ct)
    {
        var confirmed = await _scheduleClient.ConfirmSlotAsync(
            saga.ScheduleId, saga.SlotId, saga.AppointmentId!.Value, ct);
        if (confirmed is null)
            throw new SagaStepException("Failed to confirm slot — DoctorScheduleService unavailable.");

        saga.MarkSlotConfirmed();
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} step SlotConfirmed: Slot {SlotId} confirmed for appointment {AppointmentId}", saga.Id, saga.SlotId, saga.AppointmentId);
        await LogStepAsync(saga, "PaymentCreated", "SlotConfirmed", "Slot confirmed with appointment", ct: ct);
    }

    private async Task PublishNotification(
        BookingSaga saga, DateTime scheduledTime, int durationMinutes, CancellationToken ct)
    {
        var @event = new AppointmentScheduledEvent
        {
            AppointmentId = saga.AppointmentId!.Value,
            PatientId = saga.PatientId,
            DoctorId = saga.DoctorId,
            ScheduledTime = scheduledTime,
            DurationMinutes = durationMinutes
        };
        await _events.PublishAsync(@event, ct);
        await _notifications.SendNotificationAsync(@event, ct);
    }

    // ── Payment confirmation (called when payment is confirmed externally) ──

    /// <summary>Complete the saga after external payment confirmation.</summary>
    public async Task CompleteAfterPaymentAsync(Guid sagaId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByIdAsync(sagaId, ct)
            ?? throw new SagaStepException($"Saga '{sagaId}' not found.");

        if (saga.CurrentStep != BookingSagaStep.AwaitingPayment)
        {
            _logger.LogWarning("Saga {SagaId} not in AwaitingPayment state (current: {Step}), skipping", sagaId, saga.CurrentStep);
            return;
        }

        saga.MarkCompleted();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "AwaitingPayment", "Completed", "Payment confirmed, booking complete", ct: ct);

        // Get appointment to retrieve scheduled time for notification
        var appointment = await _appointmentRepo.GetByIdAsync(saga.AppointmentId!.Value, ct);
        if (appointment is not null)
        {
            await PublishNotification(saga, appointment.ScheduledTime, appointment.DurationMinutes, ct);
        }

        _logger.LogInformation("Saga {SagaId} completed after payment confirmation. Appointment {AppointmentId}",
            saga.Id, saga.AppointmentId);
    }

    /// <summary>Cancel booking if payment times out.</summary>
    public async Task CancelExpiredAsync(Guid sagaId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByIdAsync(sagaId, ct);
        if (saga is null || saga.CurrentStep != BookingSagaStep.AwaitingPayment) return;

        _logger.LogWarning("Saga {SagaId} payment timed out, compensating", sagaId);
        saga.MarkFailed("Payment timed out");
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "AwaitingPayment", "Failed", "Payment timed out", ct: ct);

        await CompensateAsync(saga, ct);
    }

    // ── Compensation with retry + outbox fallback ─────────────────────────

    private async Task CompensateAsync(BookingSaga saga, CancellationToken ct)
    {
        saga.MarkCompensating();
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} compensating", saga.Id);

        // Refund payment
        if (saga.PaymentId.HasValue)
        {
            var refunded = await RetryWithFallback(
                saga, "RefundPayment",
                () => _paymentClient.RefundPaymentAsync(saga.PaymentId.Value, ct),
                JsonSerializer.Serialize(new { PaymentId = saga.PaymentId.Value }),
                ct);

            _logger.LogInformation("Saga {SagaId} compensation: {Result} payment {PaymentId}",
                saga.Id, refunded ? "Refunded" : "Queued refund for", saga.PaymentId);
            await LogStepAsync(saga, "Compensating", "Compensating",
                refunded ? $"Refunded payment {saga.PaymentId}" : $"Refund payment {saga.PaymentId} queued for retry",
                ct: ct);
        }

        // Release slot
        if (saga.AppointmentId.HasValue)
        {
            var released = await RetryWithFallback(
                saga, "ReleaseSlot",
                () => _scheduleClient.ReleaseSlotAsync(saga.ScheduleId, saga.SlotId, ct),
                JsonSerializer.Serialize(new { saga.ScheduleId, saga.SlotId }),
                ct);

            _logger.LogInformation("Saga {SagaId} compensation: {Result} slot {SlotId}",
                saga.Id, released ? "Released" : "Queued release for", saga.SlotId);
            await LogStepAsync(saga, "Compensating", "Compensating",
                released ? $"Released slot {saga.SlotId}" : $"Release slot {saga.SlotId} queued for retry",
                ct: ct);
        }

        // Cancel appointment (local — always succeeds)
        if (saga.AppointmentId.HasValue)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(saga.AppointmentId.Value, ct);
            if (appointment is not null)
            {
                appointment.Cancel();
                await _appointmentRepo.SaveChangesAsync(ct);
                _logger.LogInformation("Saga {SagaId} compensation: Cancelled appointment {AppointmentId}", saga.Id, saga.AppointmentId);
                await LogStepAsync(saga, "Compensating", "Compensating",
                    $"Cancelled appointment {saga.AppointmentId}", ct: ct);
            }
        }

        saga.MarkCompensated();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "Compensating", "Compensated", "Compensation complete", ct: ct);
        _logger.LogInformation("Saga {SagaId} compensation complete", saga.Id);
    }

    /// <summary>
    /// Try action with immediate retries. If all retries fail, save to outbox
    /// for background worker to pick up later.
    /// </summary>
    private async Task<bool> RetryWithFallback(
        BookingSaga saga, string actionType, Func<Task<bool>> action,
        string payload, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxImmediateRetries; attempt++)
        {
            try
            {
                var success = await action();
                if (success) return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Saga {SagaId} {Action} attempt {Attempt}/{Max} failed: {Error}",
                    saga.Id, actionType, attempt, MaxImmediateRetries, ex.Message);
            }

            if (attempt < MaxImmediateRetries)
                await Task.Delay(RetryDelay * attempt, ct);
        }

        // All immediate retries failed — save to outbox for background retry
        _logger.LogWarning("Saga {SagaId} {Action} failed after {Max} retries — saving to outbox",
            saga.Id, actionType, MaxImmediateRetries);

        var outboxItem = CompensationOutbox.Create(saga.Id, actionType, payload);
        await _outboxRepo.AddAsync(outboxItem, ct);
        await _outboxRepo.SaveChangesAsync(ct);

        return false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task LogStepAsync(
        BookingSaga saga, string fromStep, string toStep,
        string? message = null, string? details = null, CancellationToken ct = default)
    {
        var log = BookingSagaLog.Create(saga.Id, fromStep, toStep, message, details);
        await _logRepo.AddAsync(log, ct);
        await _logRepo.SaveChangesAsync(ct);
    }
}

/// <summary>Thrown when a saga step fails, triggering compensation.</summary>
public class SagaStepException : Exception
{
    public SagaStepException(string message) : base(message) { }
}
