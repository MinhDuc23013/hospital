using System.Text.Json;
using HospitalShared.Events;
using PharmacyServiceDotnet.Application.Services;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Domain.Enums;
using PharmacyServiceDotnet.Domain.Exceptions;
using PharmacyServiceDotnet.Infrastructure.HttpClients;
using PharmacyServiceDotnet.Infrastructure.MessageBus;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Saga;

/// <summary>
/// Saga orchestrator for the prescription dispensing flow.
/// Flow: CreatePrescription → ReserveStock(FEFO) → CreatePayment → AwaitPayment → CommitStock → Dispense.
/// Compensation: ReleaseReservations + CancelPayment + CancelPrescription.
/// </summary>
public class DispensingSagaOrchestrator
{
    private readonly IDispensingSagaRepository _sagaRepo;
    private readonly IDispensingSagaLogRepository _logRepo;
    private readonly IPrescriptionRepository _prescriptionRepo;
    private readonly IDrugRepository _drugRepo;
    private readonly StockReservationService _stockService;
    private readonly PaymentServiceClient _paymentClient;
    private readonly EventPublisher _events;
    private readonly ILogger<DispensingSagaOrchestrator> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public DispensingSagaOrchestrator(
        IDispensingSagaRepository sagaRepo,
        IDispensingSagaLogRepository logRepo,
        IPrescriptionRepository prescriptionRepo,
        IDrugRepository drugRepo,
        StockReservationService stockService,
        PaymentServiceClient paymentClient,
        EventPublisher events,
        ILogger<DispensingSagaOrchestrator> logger)
    {
        _sagaRepo = sagaRepo;
        _logRepo = logRepo;
        _prescriptionRepo = prescriptionRepo;
        _drugRepo = drugRepo;
        _stockService = stockService;
        _paymentClient = paymentClient;
        _events = events;
        _logger = logger;
    }

    /// <summary>Start dispensing: create prescription → reserve stock → create payment → await payment.</summary>
    public async Task<DispensingSaga> StartAsync(
        Guid patientId, string doctorId, Guid? appointmentId,
        string itemsJson, decimal paymentAmount, string? notes, CancellationToken ct)
    {
        // Create prescription
        var prescription = Prescription.Create(patientId, doctorId, appointmentId, itemsJson, notes);
        await _prescriptionRepo.AddAsync(prescription, ct);
        await _prescriptionRepo.SaveChangesAsync(ct);

        // Create saga
        var saga = DispensingSaga.Create(prescription.Id, patientId, doctorId, paymentAmount, notes);
        await _sagaRepo.AddAsync(saga, ct);
        await _sagaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Dispensing saga {SagaId} started for prescription {PrescriptionId}", saga.Id, prescription.Id);

        try
        {
            // Step 1: Mark prescription created
            saga.MarkPrescriptionCreated();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "Started", "PrescriptionCreated", $"Prescription {prescription.Id} created", ct);

            // Step 2: Reserve stock using FEFO
            var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(itemsJson, JsonOpts) ?? [];
            await _stockService.ReserveAsync(prescription.Id, patientId, items, ct);
            saga.MarkStockReserved();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "PrescriptionCreated", "StockReserved", "Stock reserved via FEFO", ct);

            // Step 3: Create payment via PaymentService
            var payment = await _paymentClient.CreatePaymentAsync(
                prescription.Id, patientId, paymentAmount, "VND", "cash",
                $"Dispensing prescription #{prescription.Id}", ct);
            if (payment is null)
                throw new DomainException("Failed to create payment — PaymentService unavailable.");

            saga.MarkPaymentCreated(payment.Id);
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "StockReserved", "PaymentCreated", $"Payment {payment.Id} created (Pending)", ct);

            // Step 4: Await external payment confirmation (webhook/callback)
            saga.MarkAwaitingPayment();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "PaymentCreated", "AwaitingPayment", "Waiting for payment confirmation", ct);

            _logger.LogInformation("Dispensing saga {SagaId} awaiting payment {PaymentId}", saga.Id, payment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dispensing saga {SagaId} failed: {Reason}", saga.Id, ex.Message);
            saga.MarkFailed(ex.Message);
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, saga.CurrentStep.ToString(), "Failed", ex.Message, ct);
            await CompensateAsync(saga, ct);
        }

        return saga;
    }

    /// <summary>Complete the saga after external payment confirmation.</summary>
    public async Task<DispensingSaga> CompletePaymentAsync(Guid sagaId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByIdAsync(sagaId, ct)
            ?? throw new NotFoundException("DispensingSaga", sagaId);

        if (saga.CurrentStep != DispensingSagaStep.AwaitingPayment)
        {
            _logger.LogWarning("Saga {SagaId} not in AwaitingPayment (current: {Step}), skipping", sagaId, saga.CurrentStep);
            return saga;
        }

        try
        {
            // Step 5: Process + complete payment via PaymentService
            if (saga.PaymentId.HasValue)
            {
                await _paymentClient.ProcessPaymentAsync(saga.PaymentId.Value, ct);
                var txId = $"DISP-{saga.Id}";
                var completed = await _paymentClient.CompletePaymentAsync(saga.PaymentId.Value, txId, ct);
                if (completed is null)
                    throw new DomainException($"Failed to complete payment {saga.PaymentId}.");
            }

            saga.MarkPaymentCompleted();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "AwaitingPayment", "PaymentCompleted", $"Payment {saga.PaymentId} confirmed", ct);

            // Step 6: Commit stock reservations (deduct actual inventory)
            await _stockService.CommitAsync(saga.PrescriptionId, saga.PatientId, ct);
            saga.MarkStockCommitted();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "PaymentCompleted", "StockCommitted", "Stock deducted from batches", ct);

            // Step 7: Dispense prescription
            var prescription = await _prescriptionRepo.GetByIdAsync(saga.PrescriptionId, ct);
            prescription?.Dispense();
            await UpdateDrugTotalsAndPublishEvents(saga, ct);

            saga.MarkDispensed();
            await _sagaRepo.SaveChangesAsync(ct);
            await LogStepAsync(saga, "StockCommitted", "Dispensed", "Prescription dispensed", ct);

            _logger.LogInformation("Dispensing saga {SagaId} completed. Prescription {PrescriptionId} dispensed", saga.Id, saga.PrescriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Saga {SagaId} failed during payment completion: {Reason}", saga.Id, ex.Message);
            saga.MarkFailed(ex.Message);
            await _sagaRepo.SaveChangesAsync(ct);
            await CompensateAsync(saga, ct);
        }

        return saga;
    }

    /// <summary>Cancel dispensing — release all reservations and cancel payment.</summary>
    public async Task<DispensingSaga> CancelAsync(Guid sagaId, CancellationToken ct)
    {
        var saga = await _sagaRepo.GetByIdAsync(sagaId, ct)
            ?? throw new NotFoundException("DispensingSaga", sagaId);

        _logger.LogWarning("Dispensing saga {SagaId} cancelled", sagaId);
        saga.MarkFailed("Cancelled by user or timeout");
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, saga.CurrentStep.ToString(), "Failed", "Cancelled", ct);
        await CompensateAsync(saga, ct);
        return saga;
    }

    // ── Compensation ──────────────────────────────────────────────────────

    private async Task CompensateAsync(DispensingSaga saga, CancellationToken ct)
    {
        saga.MarkCompensating();
        await _sagaRepo.SaveChangesAsync(ct);
        _logger.LogInformation("Saga {SagaId} compensating", saga.Id);

        try
        {
            // Release stock reservations
            await _stockService.ReleaseAsync(saga.PrescriptionId, saga.PatientId, ct);
            await LogStepAsync(saga, "Compensating", "Compensating", "Stock reservations released", ct);

            // Cancel payment if created
            if (saga.PaymentId.HasValue)
            {
                await _paymentClient.CancelPaymentAsync(saga.PaymentId.Value, ct);
                await LogStepAsync(saga, "Compensating", "Compensating", $"Payment {saga.PaymentId} cancelled", ct);
            }

            // Cancel prescription
            var prescription = await _prescriptionRepo.GetByIdAsync(saga.PrescriptionId, ct);
            if (prescription is not null)
            {
                prescription.Cancel();
                await _prescriptionRepo.SaveChangesAsync(ct);
                await LogStepAsync(saga, "Compensating", "Compensating", $"Prescription {saga.PrescriptionId} cancelled", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga {SagaId} compensation failed: {Error}", saga.Id, ex.Message);
            await LogStepAsync(saga, "Compensating", "Compensating", $"Compensation failed: {ex.Message}", ct);
        }

        saga.MarkCompensated();
        await _sagaRepo.SaveChangesAsync(ct);
        await LogStepAsync(saga, "Compensating", "Compensated", "Compensation complete", ct);
        _logger.LogInformation("Saga {SagaId} compensation complete", saga.Id);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task UpdateDrugTotalsAndPublishEvents(DispensingSaga saga, CancellationToken ct)
    {
        var prescription = await _prescriptionRepo.GetByIdAsync(saga.PrescriptionId, ct);
        if (prescription is null) return;

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.Items, JsonOpts) ?? [];
        foreach (var item in items)
        {
            var drug = await _drugRepo.GetByIdAsync(item.DrugId, ct);
            if (drug is null) continue;

            drug.Update(null, null, drug.Quantity - item.Quantity, null, null);

            await _events.PublishAsync(new DrugDispensedEvent
            {
                DrugId = drug.Id.ToString(),
                DrugName = drug.Name,
                BatchNumber = "multi-batch",
                PatientId = saga.PatientId.ToString(),
                PrescriptionId = saga.PrescriptionId.ToString(),
                Quantity = item.Quantity,
                DispensedBy = saga.DoctorId
            }, ct);

            if (drug.Quantity < drug.LowStockThreshold)
            {
                await _events.PublishAsync(new InventoryLowEvent
                {
                    DrugId = drug.Id.ToString(),
                    DrugName = drug.Name,
                    CurrentStock = drug.Quantity,
                    MinimumStock = drug.LowStockThreshold
                }, ct);
            }
        }

        await _prescriptionRepo.SaveChangesAsync(ct);
    }

    private async Task LogStepAsync(
        DispensingSaga saga, string fromStep, string toStep,
        string? message = null, CancellationToken ct = default)
    {
        var log = DispensingSagaLog.Create(saga.Id, fromStep, toStep, message);
        await _logRepo.AddAsync(log, ct);
        await _logRepo.SaveChangesAsync(ct);
    }
}
