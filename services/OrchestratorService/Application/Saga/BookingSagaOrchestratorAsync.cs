using HospitalShared.Events;
using OrchestratorService.Domain.Enums;

namespace OrchestratorService.Application.Saga;

/// <summary>
/// Async phase of the booking saga.
/// Called by BookingAsyncPhaseConsumer after the sync phase returns 202.
///
/// Steps (idempotent — safe to replay on consumer retry):
///   BookingConfirmed  → ConfirmSlot in DoctorScheduleService (critical, triggers compensation on failure)
///   ConfirmAppointment → POST /api/appointments/{id}/confirm to AppointmentService (critical)
///   NotificationSent  → Publish AppointmentScheduledEvent to RabbitMQ (best-effort)
///   SearchIndexed     → Stage AppointmentBookedEvent to outbox → Kafka → SearchService (best-effort)
///   Completed         → Saga done
/// </summary>
public partial class BookingSagaOrchestrator
{
    public async Task ExecuteAsyncPhaseAsync(Guid sagaId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByIdAsync(sagaId, ct);
        if (saga is null)
        {
            _logger.LogWarning("AsyncPhase: saga {SagaId} not found, skipping", sagaId);
            return;
        }

        // Guard: only process sagas waiting for async phase
        if (saga.CurrentStep is BookingSagaStep.Completed
            or BookingSagaStep.Failed
            or BookingSagaStep.Compensating
            or BookingSagaStep.Compensated)
        {
            _logger.LogInformation("AsyncPhase: saga {SagaId} already at {Step}, skipping", sagaId, saga.CurrentStep);
            return;
        }

        _logger.LogInformation("AsyncPhase: saga {SagaId} starting at step {Step}", sagaId, saga.CurrentStep);

        // ── Critical steps: ConfirmSlot + ConfirmAppointment ──────────────
        // Failure here triggers compensation (slot must be released, appointment cancelled)
        if (saga.CurrentStep < BookingSagaStep.BookingConfirmed)
        {
            try
            {
                await AsyncStep_ConfirmSlot(saga, ct);
                await AsyncStep_ConfirmAppointment(saga, ct);
                await _sagaRepo.SaveChangesAsync(ct);
            }
            catch (SagaStepException ex)
            {
                _logger.LogError(ex, "AsyncPhase: critical failure for saga {SagaId} — compensating", sagaId);
                saga.MarkFailed(ex.Message);
                await _sagaRepo.SaveChangesAsync(ct);
                await LogStepAsync(saga, saga.CurrentStep.ToString(), "Failed", ex.Message, ct);
                await CompensateAsync(saga, ct);
                return;
            }
        }

        // ── Best-effort steps: Notification + Search ──────────────────────
        // Failures are logged but do NOT trigger compensation — booking is already confirmed
        if (saga.CurrentStep < BookingSagaStep.NotificationSent)
            await AsyncStep_SendNotification(saga, ct);

        if (saga.CurrentStep < BookingSagaStep.SearchIndexed)
            await AsyncStep_IndexSearch(saga, ct);

        saga.MarkBookingCompleted();
        AddStepLog(saga, "SearchIndexed", "Completed", "Async phase complete");
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("AsyncPhase: saga {SagaId} fully completed. Appointment={AppointmentId}",
            sagaId, saga.AppointmentId);
    }

    // ── Async step implementations ─────────────────────────────────────────

    private async Task AsyncStep_ConfirmSlot(Domain.Entities.BookingSaga saga, CancellationToken ct)
    {
        var confirmed = await _scheduleClient.ConfirmSlotAsync(
            saga.ScheduleId, saga.SlotId, saga.AppointmentId!.Value, ct);

        if (confirmed is null)
            throw new SagaStepException("ConfirmSlot failed — DoctorScheduleService unavailable.");

        saga.MarkBookingConfirmed();
        AddStepLog(saga, "SlotReserved", "BookingConfirmed", $"Slot {saga.SlotId} confirmed");
        _logger.LogInformation("AsyncPhase: saga {SagaId} slot {SlotId} confirmed", saga.Id, saga.SlotId);
    }

    private async Task AsyncStep_ConfirmAppointment(Domain.Entities.BookingSaga saga, CancellationToken ct)
    {
        var ok = await _appointmentClient.ConfirmAppointmentAsync(saga.AppointmentId!.Value, ct);
        if (!ok)
            throw new SagaStepException($"Appointment {saga.AppointmentId} confirm failed — AppointmentService unavailable.");

        _logger.LogDebug("AsyncPhase: saga {SagaId} appointment {AppointmentId} confirmed via HTTP",
            saga.Id, saga.AppointmentId);
    }

    private async Task AsyncStep_SendNotification(Domain.Entities.BookingSaga saga, CancellationToken ct)
    {
        try
        {
            var @event = new AppointmentScheduledEvent
            {
                AppointmentId = saga.AppointmentId!.Value,
                PatientId = saga.PatientId,
                DoctorId = saga.DoctorId,
                ScheduledTime = saga.ScheduledTime,
                DurationMinutes = saga.DurationMinutes
            };
            await _notifications.SendNotificationAsync(@event, ct);

            saga.MarkNotificationSent();
            AddStepLog(saga, "BookingConfirmed", "NotificationSent", "Notification published");
            _logger.LogInformation("AsyncPhase: saga {SagaId} notification sent", saga.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AsyncPhase: notification failed for saga {SagaId} — continuing", saga.Id);
        }
    }

    private Task AsyncStep_IndexSearch(Domain.Entities.BookingSaga saga, CancellationToken ct)
    {
        try
        {
            _events.StageEvent(new AppointmentBookedEvent
            {
                AppointmentId = saga.AppointmentId!.Value,
                PatientId = saga.PatientId,
                DoctorId = saga.DoctorId,
                ScheduledTime = saga.ScheduledTime,
                DurationMinutes = saga.DurationMinutes,
                Status = "Confirmed"
            });

            saga.MarkSearchIndexed();
            AddStepLog(saga, "NotificationSent", "SearchIndexed", "AppointmentBookedEvent staged for Kafka");
            _logger.LogInformation("AsyncPhase: saga {SagaId} search index event staged", saga.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AsyncPhase: search indexing failed for saga {SagaId} — continuing", saga.Id);
        }
        return Task.CompletedTask;
    }
}
