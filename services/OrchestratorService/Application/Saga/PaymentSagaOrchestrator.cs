using OrchestratorService.Domain.Entities;
using OrchestratorService.Domain.Enums;
using OrchestratorService.Infrastructure.HttpClients;
using OrchestratorService.Infrastructure.Repositories;

namespace OrchestratorService.Application.Saga;

/// <summary>
/// Orchestrates the payment flow for an appointment.
///
/// Sync phase (blocks HTTP, returns checkout URL):
///   Step 1 — Validate invoice (get total from PaymentService)
///   Step 2 — Create payment record in PaymentService
///   Step 3 — Process payment (get checkout URL from provider)
///
/// Async phase (background, triggered via Kafka):
///   Step 4 — PaymentEventConsumer listens for PaymentCompletedEvent / PaymentFailedEvent
///   Step 5 — Mark saga Completed or Failed accordingly
/// </summary>
public class PaymentSagaOrchestrator
{
    private readonly IPaymentSagaRepository _sagaRepo;
    private readonly PaymentServiceClient _paymentClient;
    private readonly ILogger<PaymentSagaOrchestrator> _logger;

    public PaymentSagaOrchestrator(
        IPaymentSagaRepository sagaRepo,
        PaymentServiceClient paymentClient,
        ILogger<PaymentSagaOrchestrator> logger)
    {
        _sagaRepo = sagaRepo;
        _paymentClient = paymentClient;
        _logger = logger;
    }

    /// <summary>
    /// Sync phase: validate invoice, create payment, process payment.
    /// Returns immediately with checkout URL (or failure reason).
    /// </summary>
    public async Task<PaymentSaga> InitiateAsync(
        Guid appointmentId, Guid patientId,
        string method, string currency,
        CancellationToken ct)
    {
        // Idempotency: return existing Processing saga (same checkout URL)
        var existing = await _sagaRepo.GetActiveByAppointmentIdAsync(appointmentId, ct);
        if (existing is not null)
        {
            if (existing.CurrentStep == PaymentSagaStep.Completed)
                throw new SagaStepException("Payment already completed for this appointment.");
            if (existing.CurrentStep == PaymentSagaStep.Processing)
            {
                _logger.LogInformation("PaymentSaga already Processing for appointment {AppointmentId}, returning existing", appointmentId);
                return existing;
            }
        }

        var saga = PaymentSaga.Create(appointmentId, patientId, method, currency);
        await _sagaRepo.AddAsync(saga, ct);
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("PaymentSaga {SagaId} started for appointment {AppointmentId}", saga.Id, appointmentId);

        try
        {
            // Step 1: Get invoice total from PaymentService
            var invoiceTotal = await _paymentClient.GetInvoiceTotalAsync(appointmentId, patientId, ct);
            if (invoiceTotal is null or <= 0)
                throw new SagaStepException("Failed to get invoice — no billable items found.");

            // Step 2: Create payment record
            var payment = await _paymentClient.CreatePaymentAsync(
                appointmentId, patientId, invoiceTotal.Value, currency, method, ct: ct);
            if (payment is null)
                throw new SagaStepException("Failed to create payment in PaymentService.");

            // Step 3: Process payment (calls fake provider, gets checkout URL)
            var processed = await _paymentClient.ProcessPaymentAsync(payment.Id, ct);
            if (processed is null)
                throw new SagaStepException("Failed to process payment — provider unavailable.");

            saga.MarkProcessing(payment.Id, invoiceTotal.Value, processed.CheckoutUrl);
            await _sagaRepo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "PaymentSaga {SagaId} → Processing. PaymentId={PaymentId}, Amount={Amount}, CheckoutUrl={Url}",
                saga.Id, payment.Id, invoiceTotal.Value, processed.CheckoutUrl);
        }
        catch (SagaStepException ex)
        {
            _logger.LogWarning("PaymentSaga {SagaId} failed at sync phase: {Reason}", saga.Id, ex.Message);
            saga.MarkFailed(ex.Message);
            await _sagaRepo.SaveChangesAsync(ct);
        }

        return saga;
    }

    /// <summary>Async phase — called by PaymentEventConsumer when PaymentCompletedEvent arrives.</summary>
    public async Task HandleCompletedAsync(Guid paymentId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByPaymentIdAsync(paymentId, ct);
        if (saga is null)
        {
            _logger.LogWarning("PaymentSaga not found for PaymentId={PaymentId}", paymentId);
            return;
        }

        if (saga.CurrentStep == PaymentSagaStep.Completed)
        {
            _logger.LogDebug("PaymentSaga {SagaId} already Completed — skipping (idempotent)", saga.Id);
            return;
        }

        saga.MarkCompleted();
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("PaymentSaga {SagaId} → Completed for appointment {AppointmentId}",
            saga.Id, saga.AppointmentId);
    }

    /// <summary>Async phase — called by PaymentEventConsumer when PaymentFailedEvent arrives.</summary>
    public async Task HandleFailedAsync(Guid paymentId, string? reason, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByPaymentIdAsync(paymentId, ct);
        if (saga is null)
        {
            _logger.LogWarning("PaymentSaga not found for PaymentId={PaymentId}", paymentId);
            return;
        }

        if (saga.CurrentStep == PaymentSagaStep.Failed)
        {
            _logger.LogDebug("PaymentSaga {SagaId} already Failed — skipping (idempotent)", saga.Id);
            return;
        }

        saga.MarkFailed(reason ?? "Payment provider reported failure");
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("PaymentSaga {SagaId} → Failed for appointment {AppointmentId}: {Reason}",
            saga.Id, saga.AppointmentId, reason);
    }
}
