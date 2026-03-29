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
            var url = "api/payments";
            _logger.LogInformation("CreatePayment calling {BaseAddress}{Url}", _http.BaseAddress, url);
            var response = await _http.PostAsJsonAsync(url, new
            {
                appointmentId, patientId, amount, currency, method, description
            }, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("CreatePayment failed {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePayment exception for appointment {AppointmentId}", appointmentId);
            return null;
        }
    }

    /// <summary>Process (charge) a pending payment.</summary>
    public async Task<PaymentDto?> ProcessPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/payments/{paymentId}/process";
            _logger.LogInformation("ProcessPayment calling {BaseAddress}{Url}", _http.BaseAddress, url);
            var response = await _http.PostAsync(url, null, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("ProcessPayment failed {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessPayment exception for payment {PaymentId}", paymentId);
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
