using Microsoft.AspNetCore.Mvc;

namespace FakePaymentProvider.Controllers;

public record InitiatePaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Method,
    string WebhookUrl,
    string ReturnUrl,
    string ProviderSecret
);

public record InitiatePaymentResponse(string TransactionId, string CheckoutUrl);

[ApiController]
[Route("api/payments")]
public class PaymentInitiateController : ControllerBase
{
    private readonly PendingTransactionStore _store;
    private readonly ILogger<PaymentInitiateController> _logger;

    public PaymentInitiateController(PendingTransactionStore store, ILogger<PaymentInitiateController> logger)
    {
        _store = store;
        _logger = logger;
    }

    [HttpPost("initiate")]
    public IActionResult Initiate([FromBody] InitiatePaymentRequest req)
    {
        var txId = $"FAKE-{Guid.NewGuid():N}";
        var tx = new PendingTransaction(txId, req.PaymentId, req.Amount, req.Currency,
            req.Method, req.WebhookUrl, req.ReturnUrl, req.ProviderSecret);
        _store.Add(tx);

        var checkoutUrl = $"{Request.Scheme}://{Request.Host}/checkout/{txId}";
        _logger.LogInformation("Initiated fake payment {TxId} for PaymentId={PaymentId} Amount={Amount}",
            txId, req.PaymentId, req.Amount);

        return Ok(new InitiatePaymentResponse(txId, checkoutUrl));
    }
}
