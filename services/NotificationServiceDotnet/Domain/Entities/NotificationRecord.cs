namespace NotificationServiceDotnet.Domain.Entities;

/// <summary>
/// Tracks sent notifications for audit and retry purposes.
/// </summary>
public class NotificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Channel { get; set; } = string.Empty; // "email" or "sms"
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
    public string? ErrorMessage { get; set; }
    public string? ReferenceId { get; set; } // e.g. AppointmentId
    public string? ReferenceType { get; set; } // e.g. "AppointmentScheduled"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
