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
/// Handles compensation (rollback) on failure at each step.
/// </summary>
public class BookingSagaOrchestrator
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IBookingSagaRepository _sagaRepo;
    private readonly IBookingSagaLogRepository _logRepo;
    private readonly PatientServiceClient _patientClient;
    private readonly DoctorScheduleServiceClient _scheduleClient;
    private readonly PaymentServiceClient _paymentClient;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly ILogger<BookingSagaOrchestrator> _logger;

    public BookingSagaOrchestrator(
        IAppointmentRepository appointmentRepo,
        IBookingSagaRepository sagaRepo,
        IBookingSagaLogRepository logRepo,
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
        // Initialize saga state
        var saga = BookingSaga.Create(patientId, providerId, scheduleId, slotId, paymentAmount, paymentMethod, notes);
        await _sagaRepo.AddAsync(saga, ct);
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Saga {SagaId} started for patient {PatientId}", saga.Id, patientId);

        try
        {
            // Step 1: Validate patient + create appointment
            await StepCreateAppointment(saga, patientId, providerId, scheduledTime, durationMinutes, notes, ct);

            // Step 2: Reserve slot
            await StepReserveSlot(saga, ct);

            // Step 3: Create + process payment
            await StepProcessPayment(saga, paymentAmount, currency, paymentMethod, ct);

            // Step 4: Confirm slot (link to appointment)
            await StepConfirmSlot(saga, ct);

            // Step 5: Mark completed + send notification
            saga.MarkCompleted();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "SlotConfirmed", "Completed", "Booking saga completed successfully", ct: ct);

            await PublishNotification(saga, scheduledTime, durationMinutes, ct);

            _logger.LogInformation("Saga {SagaId} completed successfully. Appointment {AppointmentId}", saga.Id, saga.AppointmentId);
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

    // ── Individual Steps ──────────────────────────────────────────────────

    private async Task StepCreateAppointment(
        BookingSaga saga, Guid patientId, string providerId,
        DateTime scheduledTime, int durationMinutes, string? notes, CancellationToken ct)
    {
        var patient = await _patientClient.GetPatientAsync(patientId, ct);
        if (patient is null)
            throw new SagaStepException("Patient not found or PatientService unavailable.");

        var appointment = Appointment.Create(patientId, providerId, scheduledTime, durationMinutes, notes);
        await _appointmentRepo.AddAsync(appointment, ct);
        await _appointmentRepo.SaveChangesAsync(ct);

        saga.MarkAppointmentCreated(appointment.Id);
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "Started", "AppointmentCreated", $"Appointment {appointment.Id} created", ct: ct);
    }

    private async Task StepReserveSlot(BookingSaga saga, CancellationToken ct)
    {
        var slot = await _scheduleClient.ReserveSlotAsync(saga.ScheduleId, saga.SlotId, saga.PatientId, ct);
        if (slot is null)
            throw new SagaStepException("Failed to reserve slot — DoctorScheduleService unavailable or slot taken.");

        saga.MarkSlotReserved();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "AppointmentCreated", "SlotReserved", $"Slot {saga.SlotId} reserved", ct: ct);
    }

    private async Task StepProcessPayment(
        BookingSaga saga, decimal amount, string currency, string method, CancellationToken ct)
    {
        var payment = await _paymentClient.CreatePaymentAsync(
            saga.AppointmentId!.Value, saga.PatientId, amount, currency, method,
            $"Appointment booking #{saga.AppointmentId}", ct);

        if (payment is null)
            throw new SagaStepException("Failed to create payment — PaymentService unavailable.");

        var processed = await _paymentClient.ProcessPaymentAsync(payment.Id, ct);
        if (processed is null || processed.Status != "Completed")
            throw new SagaStepException($"Payment processing failed for payment {payment.Id}.");

        saga.MarkPaymentProcessed(payment.Id);
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "SlotReserved", "PaymentProcessed", $"Payment {payment.Id} processed", ct: ct);
    }

    private async Task StepConfirmSlot(BookingSaga saga, CancellationToken ct)
    {
        var confirmed = await _scheduleClient.ConfirmSlotAsync(
            saga.ScheduleId, saga.SlotId, saga.AppointmentId!.Value, ct);

        if (confirmed is null)
            throw new SagaStepException("Failed to confirm slot — DoctorScheduleService unavailable.");

        saga.MarkSlotConfirmed();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "PaymentProcessed", "SlotConfirmed", "Slot confirmed with appointment", ct: ct);
    }

    private async Task PublishNotification(
        BookingSaga saga, DateTime scheduledTime, int durationMinutes, CancellationToken ct)
    {
        var @event = new AppointmentScheduledEvent
        {
            AppointmentId = saga.AppointmentId!.Value,
            PatientId = saga.PatientId,
            ProviderId = saga.ProviderId,
            ScheduledTime = scheduledTime,
            DurationMinutes = durationMinutes
        };

        // Kafka — audit/trace log
        await _events.PublishAsync(@event, ct);

        // RabbitMQ — trigger SMS/email via NotificationService
        await _notifications.SendNotificationAsync(@event, ct);
    }

    // ── Compensation ──────────────────────────────────────────────────────

    private async Task CompensateAsync(BookingSaga saga, CancellationToken ct)
    {
        saga.MarkCompensating();
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Saga {SagaId} compensating from step {Step}", saga.Id, saga.CurrentStep);

        // Compensate in reverse order based on what was completed
        if (saga.PaymentId.HasValue)
        {
            _logger.LogInformation("Saga {SagaId} → refunding payment {PaymentId}", saga.Id, saga.PaymentId);
            await _paymentClient.RefundPaymentAsync(saga.PaymentId.Value, ct);
            await LogStepAsync(saga, "Compensating", "Compensating", $"Refunded payment {saga.PaymentId}", ct: ct);
        }

        if (saga.CurrentStep >= BookingSagaStep.Failed && saga.SlotId != Guid.Empty)
        {
            // Only release if slot was reserved (step >= SlotReserved was reached before failure)
            var wasSlotReserved = saga.AppointmentId.HasValue; // SlotReserved comes after AppointmentCreated
            if (wasSlotReserved)
            {
                _logger.LogInformation("Saga {SagaId} → releasing slot {SlotId}", saga.Id, saga.SlotId);
                await _scheduleClient.ReleaseSlotAsync(saga.ScheduleId, saga.SlotId, ct);
                await LogStepAsync(saga, "Compensating", "Compensating", $"Released slot {saga.SlotId}", ct: ct);
            }
        }

        if (saga.AppointmentId.HasValue)
        {
            _logger.LogInformation("Saga {SagaId} → cancelling appointment {AppointmentId}", saga.Id, saga.AppointmentId);
            var appointment = await _appointmentRepo.GetByIdAsync(saga.AppointmentId.Value, ct);
            if (appointment is not null)
            {
                appointment.Cancel();
                await _appointmentRepo.SaveChangesAsync(ct);
                await LogStepAsync(saga, "Compensating", "Compensating", $"Cancelled appointment {saga.AppointmentId}", ct: ct);
            }
        }

        saga.MarkCompensated();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "Compensating", "Compensated", "All compensations applied", ct: ct);

        _logger.LogInformation("Saga {SagaId} compensation complete", saga.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

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
