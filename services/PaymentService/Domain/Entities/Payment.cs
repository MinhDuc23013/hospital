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

    // Cash-specific fields (null for non-cash payments)
    public decimal? AmountReceived { get; private set; }       // Tiền patient đưa
    public decimal? ChangeReturned { get; private set; }       // Tiền thối = AmountReceived - Amount
    public string? CashierId { get; private set; }             // Keycloak user ID của cashier
    public Guid? CashSessionId { get; private set; }           // Link vào ca thu ngân
    public string? ReceiptNumber { get; private set; }         // Số biên lai liên tục, format: HĐ/YYYY/NNNNN

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
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>Transition to Processing state — payment is being handled by provider.</summary>
    public void Process()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot process payment in status '{Status}'.");
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Mark payment as completed with an external transaction reference.</summary>
    public void Complete(string transactionId)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException($"Cannot complete payment in status '{Status}'.");
        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        PaidAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Mark payment as failed.</summary>
    public void Fail()
    {
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Cancel a pending/processing payment (no money was charged).</summary>
    public void Cancel()
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new DomainException($"Cannot cancel payment in status '{Status}'.");
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// Complete a CASH payment in one step (at cashier counter).
    /// Validates amount received ≥ total, calculates change automatically.
    /// </summary>
    public void CompleteCash(decimal amountReceived, string cashierId, Guid cashSessionId, string receiptNumber)
    {
        if (Method != PaymentMethod.Cash)
            throw new DomainException($"CompleteCash only valid for Cash payments, got '{Method}'.");
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot complete cash payment in status '{Status}'.");
        if (amountReceived < Amount)
            throw new DomainException($"Amount received ({amountReceived}) is less than total ({Amount}).");

        AmountReceived = amountReceived;
        ChangeReturned = amountReceived - Amount;
        CashierId = cashierId;
        CashSessionId = cashSessionId;
        ReceiptNumber = receiptNumber;
        TransactionId = receiptNumber; // Use receipt as transaction reference
        Status = PaymentStatus.Completed;
        PaidAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Refund a completed payment.</summary>
    public void Refund()
    {
        if (Status != PaymentStatus.Completed)
            throw new DomainException($"Cannot refund payment in status '{Status}'. Only Completed payments can be refunded.");
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.Now;
    }
}
