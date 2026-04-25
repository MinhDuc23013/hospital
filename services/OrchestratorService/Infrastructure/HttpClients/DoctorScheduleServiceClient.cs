using HospitalShared.DTOs;
using System.Net.Http.Json;

namespace OrchestratorService.Infrastructure.HttpClients;

/// <summary>HTTP client for interacting with DoctorScheduleService (reserve/confirm/release slots).</summary>
public class DoctorScheduleServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<DoctorScheduleServiceClient> _logger;

    public DoctorScheduleServiceClient(HttpClient http, ILogger<DoctorScheduleServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Reserve a time slot for a patient.</summary>
    public async Task<TimeSlotDto?> ReserveSlotAsync(
        Guid scheduleId, Guid slotId, Guid patientId, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/doctor-schedules/{scheduleId}/slots/{slotId}/reserve";
            _logger.LogInformation("ReserveSlot calling {BaseAddress}{Url}", _http.BaseAddress, url);
            var response = await _http.PostAsJsonAsync(url, new { patientId }, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("ReserveSlot failed {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<TimeSlotDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReserveSlot exception for slot {SlotId} on schedule {ScheduleId}", slotId, scheduleId);
            return null;
        }
    }

    /// <summary>Confirm a reserved slot by linking it to an appointment.</summary>
    public async Task<TimeSlotDto?> ConfirmSlotAsync(
        Guid scheduleId, Guid slotId, Guid appointmentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"api/doctor-schedules/{scheduleId}/slots/{slotId}/confirm",
                new { appointmentId }, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TimeSlotDto>(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to confirm slot {SlotId}", slotId);
            return null;
        }
    }

    /// <summary>Release a reserved slot (compensation).</summary>
    public async Task<bool> ReleaseSlotAsync(
        Guid scheduleId, Guid slotId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.DeleteAsync(
                $"api/doctor-schedules/{scheduleId}/slots/{slotId}/reservation", ct);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to release slot {SlotId} (compensation)", slotId);
            return false;
        }
    }
}
