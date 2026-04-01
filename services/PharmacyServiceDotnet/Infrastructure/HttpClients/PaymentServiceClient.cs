using HospitalShared.DTOs;
using System.Net.Http.Json;

namespace PharmacyServiceDotnet.Infrastructure.HttpClients;

/// <summary>HTTP client for interacting with PaymentService from PharmacyService.</summary>
public class PaymentServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient http, ILogger<PaymentServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Create a pending payment for a prescription dispensing.</summary>
    public async Task<PaymentDto?> CreatePaymentAsync(
        Guid prescriptionId, Guid patientId,
        decimal amount, string currency, string method, string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/payments", new
            {
                appointmentId = prescriptionId, // reuse field for prescription context
                patientId, amount, currency, method, description
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
            _logger.LogError(ex, "CreatePayment exception for prescription {PrescriptionId}", prescriptionId);
            return null;
        }
    }

    /// <summary>Process (charge) a pending payment.</summary>
    public async Task<PaymentDto?> ProcessPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"api/payments/{paymentId}/process", null, ct);
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

    /// <summary>Complete a payment after external confirmation.</summary>
    public async Task<PaymentDto?> CompletePaymentAsync(Guid paymentId, string transactionId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/payments/{paymentId}/complete", new { transactionId }, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("CompletePayment failed {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<PaymentDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompletePayment exception for payment {PaymentId}", paymentId);
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
            _logger.LogWarning(ex, "Failed to refund payment {PaymentId}", paymentId);
            return false;
        }
    }

    /// <summary>Cancel a pending payment.</summary>
    public async Task<bool> CancelPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"api/payments/{paymentId}/cancel", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelPayment exception for payment {PaymentId}", paymentId);
            return false;
        }
    }
}
