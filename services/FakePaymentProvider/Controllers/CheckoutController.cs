using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace FakePaymentProvider.Controllers;

[ApiController]
[Route("checkout")]
public class CheckoutController : ControllerBase
{
    private readonly PendingTransactionStore _store;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(PendingTransactionStore store, IHttpClientFactory httpClientFactory,
        ILogger<CheckoutController> logger)
    {
        _store = store;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("{transactionId}")]
    public ContentResult Show(string transactionId)
    {
        var tx = _store.Get(transactionId);
        if (tx is null)
            return Content("<h1>Transaction not found</h1>", "text/html");

        var amount = tx.Amount.ToString("N0");
        var html = $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="UTF-8"/>
              <title>Thanh toán giả lập</title>
              <style>
                body { font-family: sans-serif; max-width: 480px; margin: 60px auto; padding: 0 20px; }
                .card { border: 1px solid #ddd; border-radius: 8px; padding: 24px; }
                h2 { margin-top: 0; color: #333; }
                .amount { font-size: 2rem; font-weight: bold; color: #2563eb; margin: 12px 0; }
                .info { color: #666; margin-bottom: 24px; }
                .btn { display: block; width: 100%; padding: 14px; font-size: 1rem;
                       border: none; border-radius: 6px; cursor: pointer; margin-bottom: 12px; }
                .btn-pay { background: #16a34a; color: white; }
                .btn-fail { background: #dc2626; color: white; }
              </style>
            </head>
            <body>
              <div class="card">
                <h2>Cổng thanh toán giả lập</h2>
                <div class="info">Mã giao dịch: <strong>{{transactionId}}</strong></div>
                <div class="info">Phương thức: <strong>{{tx.Method}}</strong></div>
                <div class="amount">{{amount}} {{tx.Currency}}</div>
                <form method="post" action="/checkout/{{transactionId}}/pay">
                  <button class="btn btn-pay" type="submit">✓ Thanh toán thành công</button>
                </form>
                <form method="post" action="/checkout/{{transactionId}}/fail">
                  <button class="btn btn-fail" type="submit">✗ Giả lập thất bại</button>
                </form>
              </div>
            </body>
            </html>
            """;
        return Content(html, "text/html");
    }

    [HttpPost("{transactionId}/pay")]
    public async Task<IActionResult> Pay(string transactionId, CancellationToken ct)
    {
        var tx = _store.Get(transactionId);
        if (tx is null) return NotFound("Transaction not found");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Provider-Secret", tx.ProviderSecret);
        var payload = new { transactionId };

        var response = await client.PostAsJsonAsync(tx.WebhookUrl, payload, ct);
        _logger.LogInformation("Webhook {Url} responded {Status}", tx.WebhookUrl, response.StatusCode);

        _store.Remove(transactionId);

        var redirectUrl = $"{tx.ReturnUrl}?status=success&transactionId={transactionId}";
        return Redirect(redirectUrl);
    }

    [HttpPost("{transactionId}/fail")]
    public async Task<IActionResult> Fail(string transactionId, CancellationToken ct)
    {
        var tx = _store.Get(transactionId);
        if (tx is null) return NotFound("Transaction not found");

        // Notify PaymentService via webhook so it can publish PaymentFailedEvent
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Provider-Secret", tx.ProviderSecret);
        var payload = new { transactionId, success = false };
        try
        {
            var response = await client.PostAsJsonAsync(tx.WebhookUrl, payload, ct);
            _logger.LogInformation("Fail webhook {Url} responded {Status}", tx.WebhookUrl, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fail webhook call to {Url} failed — continuing redirect", tx.WebhookUrl);
        }

        _store.Remove(transactionId);

        var redirectUrl = $"{tx.ReturnUrl}?status=failed&transactionId={transactionId}";
        return Redirect(redirectUrl);
    }
}
