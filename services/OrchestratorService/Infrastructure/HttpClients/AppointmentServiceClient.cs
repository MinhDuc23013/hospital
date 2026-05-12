using System.Net.Http.Json;

namespace OrchestratorService.Infrastructure.HttpClients;

/// <summary>
/// HTTP client for AppointmentService CRUD operations.
/// OrchestratorService never touches appointment tables directly — all writes go through here.
/// </summary>
public class AppointmentServiceClient : IAppointmentServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AppointmentServiceClient> _logger;

    public AppointmentServiceClient(HttpClient http, ILogger<AppointmentServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Create a new appointment via AppointmentService → POST /api/appointments.</summary>
    public async Task<Guid?> CreateAppointmentAsync(
        Guid patientId, string doctorId,
        DateTime scheduledTime, int durationMinutes,
        string? notes, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                patientId,
                doctorId,
                scheduledTime,
                durationMinutes,
                notes
            };

            _logger.LogInformation("CreateAppointment calling {BaseAddress}api/appointments", _http.BaseAddress);
            var response = await _http.PostAsJsonAsync("api/appointments", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("CreateAppointment failed {StatusCode}: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AppointmentCreatedResponse>(ct);
            return result?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAppointment exception for patient {PatientId}", patientId);
            return null;
        }
    }

    /// <summary>Confirm an appointment via AppointmentService → POST /api/appointments/{id}/confirm.</summary>
    public async Task<bool> ConfirmAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"api/appointments/{appointmentId}/confirm", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("ConfirmAppointment {Id} failed {StatusCode}: {Body}",
                    appointmentId, response.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmAppointment exception for appointment {AppointmentId}", appointmentId);
            return false;
        }
    }

    /// <summary>Cancel an appointment via AppointmentService → DELETE /api/appointments/{id}.</summary>
    public async Task<bool> CancelAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/appointments/{appointmentId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("CancelAppointment {Id} failed {StatusCode}: {Body}",
                    appointmentId, response.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelAppointment exception for appointment {AppointmentId}", appointmentId);
            return false;
        }
    }

    // Internal response type for appointment creation
    private record AppointmentCreatedResponse(Guid Id);
}
