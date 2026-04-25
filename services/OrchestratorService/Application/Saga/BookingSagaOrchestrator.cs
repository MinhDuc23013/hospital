using System.Text.Json;
using OrchestratorService.Domain.Entities;
using OrchestratorService.Domain.Enums;
using OrchestratorService.Infrastructure.HttpClients;
using OrchestratorService.Infrastructure.MessageBus;
using OrchestratorService.Infrastructure.Repositories;
using HospitalShared.Events;

namespace OrchestratorService.Application.Saga;

/// <summary>
/// Hybrid Saga Orchestrator for appointment booking.
///
/// Sync phase  (blocks HTTP):  ValidatePatient → CreateAppointment → LockSlot → 202 SlotReserved
/// Async phase (background):   ConfirmSlot → ConfirmAppointment → SendNotification → IndexSearch
///
/// The sync phase stages a BookingSlotLockedEvent into the outbox (same DB flush).
/// OutboxPublishWorker pushes it to Kafka → BookingAsyncPhaseConsumer picks it up.
/// AppointmentService is called via HTTP — no direct DB access to appointment tables.
/// </summary>
public partial class BookingSagaOrchestrator
{
    private readonly IBookingSagaRepository _sagaRepo;
    private readonly IBookingSagaLogRepository _logRepo;
    private readonly ICompensationOutboxRepository _outboxRepo;
    private readonly AppointmentServiceClient _appointmentClient;
    private readonly PatientServiceClient _patientClient;
    private readonly DoctorScheduleServiceClient _scheduleClient;
    private readonly PaymentServiceClient _paymentClient;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly ILogger<BookingSagaOrchestrator> _logger;

    private const int MaxImmediateRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public BookingSagaOrchestrator(
        IBookingSagaRepository sagaRepo,
        IBookingSagaLogRepository logRepo,
        ICompensationOutboxRepository outboxRepo,
        AppointmentServiceClient appointmentClient,
        PatientServiceClient patientClient,
        DoctorScheduleServiceClient scheduleClient,
        PaymentServiceClient paymentClient,
        EventPublisher events,
        NotificationPublisher notifications,
        ILogger<BookingSagaOrchestrator> logger)
    {
        _sagaRepo = sagaRepo;
        _logRepo = logRepo;
        _outboxRepo = outboxRepo;
        _appointmentClient = appointmentClient;
        _patientClient = patientClient;
        _scheduleClient = scheduleClient;
        _paymentClient = paymentClient;
        _events = events;
        _notifications = notifications;
        _logger = logger;
    }

    // ── Sync Phase ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sync phase: validate patient, create appointment (via HTTP), lock slot.
    /// Stages BookingSlotLockedEvent in the same DB flush → 202 returned immediately.
    /// The async phase (confirm + notify + index) runs in BookingAsyncPhaseConsumer.
    /// </summary>
    public async Task<BookingSaga> ExecuteAsync(
        Guid patientId, string providerId,
        Guid scheduleId, Guid slotId,
        DateTime scheduledTime, int durationMinutes,
        decimal paymentAmount, string paymentMethod, string currency,
        string? notes, CancellationToken ct)
    {
        var saga = BookingSaga.Create(patientId, providerId, scheduleId, slotId,
            scheduledTime, durationMinutes, paymentAmount, paymentMethod, notes);
        await _sagaRepo.AddAsync(saga, ct);
        _logger.LogInformation("Saga {SagaId} started for patient {PatientId}", saga.Id, patientId);

        try
        {
            await SyncStep_ValidatePatient(saga, patientId, ct);
            await SyncStep_CreateAppointment(saga, patientId, providerId, scheduledTime, durationMinutes, notes, ct);
            await SyncStep_LockSlot(saga, ct);

            // Stage async trigger — saved atomically with saga state below
            _events.StageEvent(new BookingSlotLockedEvent
            {
                SagaId = saga.Id,
                AppointmentId = saga.AppointmentId!.Value,
                PatientId = saga.PatientId,
                DoctorId = saga.DoctorId,
                ScheduleId = saga.ScheduleId,
                SlotId = saga.SlotId,
                ScheduledTime = saga.ScheduledTime,
                DurationMinutes = saga.DurationMinutes
            });
            AddStepLog(saga, "SlotReserved", "SlotReserved", "Slot locked — async phase queued");
            await _sagaRepo.SaveChangesAsync(ct); // single flush: saga + log + outbox

            _logger.LogInformation(
                "Saga {SagaId} sync phase complete. Appointment={AppointmentId}, Slot={SlotId}",
                saga.Id, saga.AppointmentId, saga.SlotId);
        }
        catch (SagaStepException ex)
        {
            _logger.LogWarning(ex, "Saga {SagaId} sync phase failed at {Step}", saga.Id, saga.CurrentStep);
            var failedFrom = saga.CurrentStep.ToString();
            saga.MarkFailed(ex.Message);
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, failedFrom, "Failed", ex.Message, ct: ct);
            await CompensateAsync(saga, ct);
        }

        return saga;
    }

    private async Task SyncStep_ValidatePatient(BookingSaga saga, Guid patientId, CancellationToken ct)
    {
        var patient = await _patientClient.GetPatientAsync(patientId, ct);
        if (patient is null)
            throw new SagaStepException($"Patient '{patientId}' not found or PatientService unavailable.");

        _logger.LogDebug("Saga {SagaId} patient {PatientId} validated", saga.Id, patientId);
    }

    private async Task SyncStep_CreateAppointment(
        BookingSaga saga, Guid patientId, string providerId,
        DateTime scheduledTime, int durationMinutes, string? notes, CancellationToken ct)
    {
        // Calls AppointmentService HTTP API — no direct DB write
        var appointmentId = await _appointmentClient.CreateAppointmentAsync(
            patientId, providerId, scheduledTime, durationMinutes, notes, ct);

        if (appointmentId is null)
            throw new SagaStepException("Failed to create appointment — AppointmentService unavailable.");

        saga.MarkAppointmentCreated(appointmentId.Value);
        AddStepLog(saga, "Started", "AppointmentCreated", $"Appointment {appointmentId} created via HTTP");
        _logger.LogDebug("Saga {SagaId} appointment {AppointmentId} created", saga.Id, appointmentId);
    }

    private async Task SyncStep_LockSlot(BookingSaga saga, CancellationToken ct)
    {
        var slot = await _scheduleClient.ReserveSlotAsync(saga.ScheduleId, saga.SlotId, saga.PatientId, ct);
        if (slot is null)
            throw new SagaStepException("Failed to lock slot — DoctorScheduleService unavailable or slot taken.");

        saga.MarkSlotReserved();
        AddStepLog(saga, "AppointmentCreated", "SlotReserved", $"Slot {saga.SlotId} locked");
        _logger.LogDebug("Saga {SagaId} slot {SlotId} locked", saga.Id, saga.SlotId);
    }

    // ── Legacy payment flow ────────────────────────────────────────────────

    /// <summary>Complete saga after external payment webhook. Triggers notification.</summary>
    public async Task CompleteAfterPaymentAsync(Guid sagaId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByIdAsync(sagaId, ct)
            ?? throw new SagaStepException($"Saga '{sagaId}' not found.");

        if (saga.CurrentStep != BookingSagaStep.AwaitingPayment)
        {
            _logger.LogWarning("Saga {SagaId} not in AwaitingPayment (current: {Step}), skipping", sagaId, saga.CurrentStep);
            return;
        }

        if (saga.PaymentId.HasValue)
        {
            await _paymentClient.ProcessPaymentAsync(saga.PaymentId.Value, ct);
            var txId = $"SAGA-{saga.Id}";
            var completed = await _paymentClient.CompletePaymentAsync(saga.PaymentId.Value, txId, ct);
            if (completed is null)
                _logger.LogWarning("Saga {SagaId} failed to complete payment {PaymentId}", sagaId, saga.PaymentId);
        }

        saga.MarkCompleted();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "AwaitingPayment", "PaymentCompleted", "Payment confirmed", ct: ct);

        // Confirm appointment via HTTP (no direct DB access)
        if (saga.AppointmentId.HasValue)
        {
            var confirmed = await _appointmentClient.ConfirmAppointmentAsync(saga.AppointmentId.Value, ct);
            if (!confirmed)
                _logger.LogWarning("Saga {SagaId} failed to confirm appointment {AppointmentId} after payment",
                    sagaId, saga.AppointmentId);

            var scheduledEvent = new AppointmentScheduledEvent
            {
                AppointmentId = saga.AppointmentId.Value,
                PatientId = saga.PatientId,
                DoctorId = saga.DoctorId,
                ScheduledTime = saga.ScheduledTime,
                DurationMinutes = saga.DurationMinutes
            };
            await _events.PublishAsync(scheduledEvent, ct);
            await _notifications.SendNotificationAsync(scheduledEvent, ct);
        }

        _logger.LogInformation("Saga {SagaId} completed after payment", sagaId);
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

    // ── Compensation ────────────────────────────────────────────────────────

    internal async Task CompensateAsync(BookingSaga saga, CancellationToken ct)
    {
        saga.MarkCompensating();
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} compensating", saga.Id);

        if (saga.PaymentId.HasValue)
        {
            var refunded = await RetryWithFallback(
                saga, "RefundPayment",
                () => _paymentClient.RefundPaymentAsync(saga.PaymentId.Value, ct),
                JsonSerializer.Serialize(new { PaymentId = saga.PaymentId.Value }), ct);

            await LogStepAsync(saga, "Compensating", "Compensating",
                refunded ? $"Refunded payment {saga.PaymentId}" : $"Refund payment {saga.PaymentId} queued", ct: ct);
        }

        if (saga.AppointmentId.HasValue)
        {
            var released = await RetryWithFallback(
                saga, "ReleaseSlot",
                () => _scheduleClient.ReleaseSlotAsync(saga.ScheduleId, saga.SlotId, ct),
                JsonSerializer.Serialize(new { saga.ScheduleId, saga.SlotId }), ct);

            await LogStepAsync(saga, "Compensating", "Compensating",
                released ? $"Released slot {saga.SlotId}" : $"Release slot {saga.SlotId} queued", ct: ct);

            // Cancel appointment via HTTP — no direct DB write
            var cancelled = await RetryWithFallback(
                saga, "CancelAppointment",
                () => _appointmentClient.CancelAppointmentAsync(saga.AppointmentId.Value, ct),
                JsonSerializer.Serialize(new { AppointmentId = saga.AppointmentId.Value }), ct);

            await LogStepAsync(saga, "Compensating", "Compensating",
                cancelled ? $"Cancelled appointment {saga.AppointmentId}" : $"Cancel appointment {saga.AppointmentId} queued", ct: ct);
        }

        saga.MarkCompensated();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "Compensating", "Compensated", "Compensation complete", ct: ct);
        _logger.LogInformation("Saga {SagaId} compensation complete", saga.Id);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Add log to context without saving — caller batches SaveChangesAsync.</summary>
    internal void AddStepLog(BookingSaga saga, string fromStep, string toStep, string? message = null)
    {
        var log = BookingSagaLog.Create(saga.Id, fromStep, toStep, message);
        _logRepo.Add(log);
    }

    /// <summary>Add + save log immediately (compensation and async flows).</summary>
    internal async Task LogStepAsync(
        BookingSaga saga, string fromStep, string toStep,
        string? message = null, CancellationToken ct = default)
    {
        var log = BookingSagaLog.Create(saga.Id, fromStep, toStep, message);
        await _logRepo.AddAsync(log, ct);
        await _logRepo.SaveChangesAsync(ct);
    }

    /// <summary>Try action with immediate retries; on all retries exhausted saves to outbox for background retry.</summary>
    internal async Task<bool> RetryWithFallback(
        BookingSaga saga, string actionType, Func<Task<bool>> action,
        string payload, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxImmediateRetries; attempt++)
        {
            try
            {
                if (await action()) return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Saga {SagaId} {Action} attempt {A}/{Max} failed: {Err}",
                    saga.Id, actionType, attempt, MaxImmediateRetries, ex.Message);
            }
            if (attempt < MaxImmediateRetries)
                await Task.Delay(RetryDelay * attempt, ct);
        }

        _logger.LogWarning("Saga {SagaId} {Action} exhausted retries — queuing to outbox", saga.Id, actionType);
        var outboxItem = CompensationOutbox.Create(saga.Id, actionType, payload);
        await _outboxRepo.AddAsync(outboxItem, ct);
        await _outboxRepo.SaveChangesAsync(ct);
        return false;
    }
}

/// <summary>Thrown when a saga step fails, triggering compensation.</summary>
public class SagaStepException : Exception
{
    public SagaStepException(string message) : base(message) { }
}
