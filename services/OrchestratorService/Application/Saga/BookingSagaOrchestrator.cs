using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
public partial class BookingSagaOrchestrator : IBookingSagaOrchestrator
{
    private readonly IBookingSagaRepository _sagaRepo;
    private readonly IBookingSagaLogRepository _logRepo;
    private readonly ICompensationOutboxRepository _outboxRepo;
    private readonly IAppointmentServiceClient _appointmentClient;
    private readonly IPatientServiceClient _patientClient;
    private readonly IDoctorScheduleServiceClient _scheduleClient;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly ILogger<BookingSagaOrchestrator> _logger;

    private const int MaxImmediateRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public BookingSagaOrchestrator(
        IBookingSagaRepository sagaRepo,
        IBookingSagaLogRepository logRepo,
        ICompensationOutboxRepository outboxRepo,
        IAppointmentServiceClient appointmentClient,
        IPatientServiceClient patientClient,
        IDoctorScheduleServiceClient scheduleClient,
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
        string? notes, CancellationToken ct)
    {
        var saga = BookingSaga.Create(patientId, providerId, scheduleId, slotId,
            scheduledTime, durationMinutes, notes);
        await _sagaRepo.AddAsync(saga, ct);

        // Flush early so the unique constraint (ux_booking_sagas_active_slot) fires
        // before any HTTP calls — prevents orphaned appointments on race condition.
        try
        {
            await _sagaRepo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" } pg
                  && pg.ConstraintName == "ux_booking_sagas_active_slot")
        {
            var dup = await _sagaRepo.GetActiveBySlotAsync(patientId, providerId, slotId, ct);
            _logger.LogWarning(
                "Race condition blocked — returning existing saga {SagaId} for Patient={PatientId} Slot={SlotId}",
                dup!.Id, patientId, slotId);
            return dup!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist saga for Patient={PatientId} Slot={SlotId}", patientId, slotId);
            throw;
        }

        _logger.LogInformation("Saga {SagaId} started for patient {PatientId}", saga.Id, patientId);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await SyncStep_ValidatePatient(saga, patientId, ct);
            await SyncStep_CreateAppointment(saga, patientId, providerId, scheduledTime, durationMinutes, notes, ct);
            await SyncStep_LockSlot(saga, ct);

            // Stage async trigger with sagaId as Kafka key → same partition → sequential consumer processing
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
            }, saga.Id.ToString());
            AddStepLog(saga, "SlotReserved", "SlotReserved", "Slot locked — async phase queued");
            await _sagaRepo.SaveChangesAsync(ct); // single flush: saga + log + outbox

            _logger.LogInformation(
                "Saga {SagaId} sync phase complete. Appointment={AppointmentId}, Slot={SlotId}, ElapsedMs={ElapsedMs}",
                saga.Id, saga.AppointmentId, saga.SlotId, sw.ElapsedMilliseconds);
        }
        catch (SagaStepException ex)
        {
            _logger.LogWarning(ex, "Saga {SagaId} sync phase failed at {Step} after {ElapsedMs}ms",
                saga.Id, saga.CurrentStep, sw.ElapsedMilliseconds);
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

        AddStepLog(saga, "Started", "PatientValidated", $"Patient {patientId} validated");
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

    // ── Compensation ────────────────────────────────────────────────────────

    internal async Task CompensateAsync(BookingSaga saga, CancellationToken ct)
    {
        saga.MarkCompensating();
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} compensating", saga.Id);

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
