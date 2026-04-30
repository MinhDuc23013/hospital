using HospitalShared;
namespace PaymentService.Domain.Entities;

/// <summary>Audit log for payment state transitions.</summary>
public class PaymentAuditLog
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string Action { get; private set; } = string.Empty;      // Created, Processing, Completed, Failed, Refunded
    public string? OldStatus { get; private set; }
    public string NewStatus { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public string? Details { get; private set; }  // JSON details
    public DateTime Timestamp { get; private set; }

    private PaymentAuditLog() { }

    public static PaymentAuditLog Create(
        Guid paymentId,
        string action,
        string? oldStatus,
        string newStatus,
        string? message = null,
        string? details = null)
    {
        return new PaymentAuditLog
        {
            Id = GuidV7.NewGuid(),
            PaymentId = paymentId,
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Message = message,
            Details = details,
            Timestamp = DateTime.Now
        };
    }
}
