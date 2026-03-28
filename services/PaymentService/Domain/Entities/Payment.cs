using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Domain.Entities;

/// <summary>Payment aggregate root — tracks financial transactions linked to appointments.</summary>
public class Payment
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? TransactionId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Payment() { } // EF Core

    public static Payment Create(
        Guid appointmentId,
        Guid patientId,
        decimal amount,
        string currency,
        PaymentMethod method,
        string? description = null)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            PatientId = patientId,
            Amount = amount,
            Currency = currency,
            Method = method,
            Status = PaymentStatus.Pending,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Transition to Processing state — payment is being handled by provider.</summary>
    public void Process()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot process payment in status '{Status}'.");
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Mark payment as completed with an external transaction reference.</summary>
    public void Complete(string transactionId)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException($"Cannot complete payment in status '{Status}'.");
        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Mark payment as failed.</summary>
    public void Fail()
    {
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Refund a completed payment.</summary>
    public void Refund()
    {
        if (Status != PaymentStatus.Completed)
            throw new DomainException($"Cannot refund payment in status '{Status}'. Only Completed payments can be refunded.");
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
