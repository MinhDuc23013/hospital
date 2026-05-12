using HospitalShared.DTOs;

namespace OrchestratorService.Infrastructure.HttpClients;

public interface IPaymentServiceClient
{
    Task<PaymentDto?> CreatePaymentAsync(Guid appointmentId, Guid patientId,
        decimal amount, string currency, string method, string? description = null,
        CancellationToken ct = default);
    Task<PaymentDto?> ProcessPaymentAsync(Guid paymentId, CancellationToken ct = default);
    Task<PaymentDto?> CompletePaymentAsync(Guid paymentId, string transactionId, CancellationToken ct = default);
    Task<decimal?> GetInvoiceTotalAsync(Guid appointmentId, Guid patientId, CancellationToken ct = default);
    Task<bool> RefundPaymentAsync(Guid paymentId, CancellationToken ct = default);
}
