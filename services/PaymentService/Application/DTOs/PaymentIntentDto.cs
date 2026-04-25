namespace PaymentService.Application.DTOs;

/// <summary>Returned after initiating a payment — exposes info client needs to redirect/display.</summary>
public class PaymentIntentDto
{
    public Guid PaymentId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    /// <summary>Redirect URL for online payment; null for cash.</summary>
    public string? CheckoutUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
