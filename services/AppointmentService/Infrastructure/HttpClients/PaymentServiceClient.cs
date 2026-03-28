using HospitalShared.DTOs;
using System.Net.Http.Json;

namespace AppointmentService.Infrastructure.HttpClients;

/// <summary>HTTP client for interacting with PaymentService (create/process/refund).</summary>
public class PaymentServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient http, ILogger<PaymentServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Create a pending payment for an appointment.</summary>
    public async Task<PaymentDto?> CreatePaymentAsync(
        Guid appointmentId, Guid patientId,
        decimal amount, string currency, string method, string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/payments", new
            {
                appointmentId, patientId, amount, currency, method, description
            }, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to create payment for appointment {AppointmentId}", appointmentId);
            return null;
        }
    }

    /// <summary>Process (charge) a pending payment.</summary>
    public async Task<PaymentDto?> ProcessPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"api/payments/{paymentId}/process", null, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to process payment {PaymentId}", paymentId);
            return null;
        }
    }

    /// <summary>Refund a completed payment (compensation).</summary>
    public async Task<bool> RefundPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"api/payments/{paymentId}/refund", null, ct);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to refund payment {PaymentId} (compensation)", paymentId);
            return false;
        }
    }
}
