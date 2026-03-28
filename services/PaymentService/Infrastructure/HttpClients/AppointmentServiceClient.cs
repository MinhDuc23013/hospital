using HospitalShared.DTOs;
using System.Net.Http.Json;

namespace PaymentService.Infrastructure.HttpClients;

/// <summary>HTTP client for validating appointment existence before creating payments.</summary>
public class AppointmentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppointmentServiceClient> _logger;

    public AppointmentServiceClient(HttpClient httpClient, ILogger<AppointmentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AppointmentDto?> GetAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AppointmentDto>($"api/appointments/{appointmentId}", ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach AppointmentService for appointment {AppointmentId}", appointmentId);
            return null;
        }
    }
}
