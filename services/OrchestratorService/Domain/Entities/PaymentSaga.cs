using HospitalShared;
using OrchestratorService.Domain.Enums;

namespace OrchestratorService.Domain.Entities;

/// <summary>Saga state entity tracking the payment flow for an appointment.</summary>
public class PaymentSaga
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "VND";
    public decimal Amount { get; private set; }

    // Set after PaymentService creates + processes the payment
    public Guid? PaymentId { get; private set; }
    public string? CheckoutUrl { get; private set; }

    public PaymentSagaStep CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PaymentSaga() { } // EF Core

    public static PaymentSaga Create(Guid appointmentId, Guid patientId, string method, string currency = "VND")
    {
        return new PaymentSaga
        {
            Id = GuidV7.NewGuid(),
            AppointmentId = appointmentId,
            PatientId = patientId,
            Method = method,
            Currency = currency,
            CurrentStep = PaymentSagaStep.Initiated,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>Payment created and checkout URL obtained — waiting for provider confirmation.</summary>
    public void MarkProcessing(Guid paymentId, decimal amount, string? checkoutUrl)
    {
        PaymentId = paymentId;
        Amount = amount;
        CheckoutUrl = checkoutUrl;
        CurrentStep = PaymentSagaStep.Processing;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompleted()
    {
        CurrentStep = PaymentSagaStep.Completed;
        UpdatedAt = DateTime.Now;
    }

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        CurrentStep = PaymentSagaStep.Failed;
        RetryCount++;
        UpdatedAt = DateTime.Now;
    }
}
