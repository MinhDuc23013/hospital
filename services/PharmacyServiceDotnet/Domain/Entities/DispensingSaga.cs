using HospitalShared;
using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>
/// Saga state entity tracking the multi-step prescription dispensing process.
/// Flow: CreatePrescription → ReserveStock(FEFO) → AwaitPayment → CommitStock → Dispense.
/// </summary>
public class DispensingSaga
{
    public Guid Id { get; private set; }

    // Input data
    public Guid PrescriptionId { get; private set; }
    public Guid PatientId { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public decimal PaymentAmount { get; private set; }
    public string? Notes { get; private set; }

    // Step results (populated as saga progresses)
    public Guid? PaymentId { get; private set; }

    // State tracking
    public DispensingSagaStep CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DispensingSaga() { }

    public static DispensingSaga Create(
        Guid prescriptionId, Guid patientId, string doctorId,
        decimal paymentAmount, string? notes = null)
    {
        return new DispensingSaga
        {
            Id = GuidV7.NewGuid(),
            PrescriptionId = prescriptionId,
            PatientId = patientId,
            DoctorId = doctorId,
            PaymentAmount = paymentAmount,
            Notes = notes,
            CurrentStep = DispensingSagaStep.Started,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void MarkPrescriptionCreated() => AdvanceTo(DispensingSagaStep.PrescriptionCreated);
    public void MarkStockReserved() => AdvanceTo(DispensingSagaStep.StockReserved);

    public void MarkPaymentCreated(Guid paymentId)
    {
        PaymentId = paymentId;
        AdvanceTo(DispensingSagaStep.PaymentCreated);
    }

    public void MarkAwaitingPayment() => AdvanceTo(DispensingSagaStep.AwaitingPayment);
    public void MarkPaymentCompleted() => AdvanceTo(DispensingSagaStep.PaymentCompleted);
    public void MarkStockCommitted() => AdvanceTo(DispensingSagaStep.StockCommitted);
    public void MarkDispensed() => AdvanceTo(DispensingSagaStep.Dispensed);

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        CurrentStep = DispensingSagaStep.Failed;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompensating()
    {
        CurrentStep = DispensingSagaStep.Compensating;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompensated()
    {
        CurrentStep = DispensingSagaStep.Compensated;
        UpdatedAt = DateTime.Now;
    }

    private void AdvanceTo(DispensingSagaStep step)
    {
        CurrentStep = step;
        UpdatedAt = DateTime.Now;
    }
}
