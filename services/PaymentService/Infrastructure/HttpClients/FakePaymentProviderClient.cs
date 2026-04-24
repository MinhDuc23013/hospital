using System.Net.Http.Json;

namespace PaymentService.Infrastructure.HttpClients;

public class FakePaymentProviderClient : IPaymentProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<FakePaymentProviderClient> _logger;

    public FakePaymentProviderClient(HttpClient httpClient, IConfiguration config,
        ILogger<FakePaymentProviderClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<InitiatePaymentResult> InitiateAsync(
        Guid paymentId, decimal amount, string currency, string method,
        string webhookUrl, string returnUrl, CancellationToken ct = default)
    {
        var secret = _config["PaymentProvider:Secret"] ?? "dev-secret";
        var payload = new
        {
            paymentId,
            amount,
            currency,
            method,
            webhookUrl,
            returnUrl,
            providerSecret = secret
        };

        var response = await _httpClient.PostAsJsonAsync("api/payments/initiate", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InitiatePaymentResult>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty response from payment provider");

        _logger.LogInformation("Payment {PaymentId} initiated — txId={TxId}", paymentId, result.TransactionId);
        return result;
    }
}
