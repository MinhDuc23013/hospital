namespace PaymentService.Infrastructure.HttpClients;

public record InitiatePaymentResult(string TransactionId, string CheckoutUrl);

/// <summary>Contract for calling an external payment provider to initiate a payment session.</summary>
public interface IPaymentProviderClient
{
    Task<InitiatePaymentResult> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string method,
        string webhookUrl,
        string returnUrl,
        CancellationToken ct = default);
}
