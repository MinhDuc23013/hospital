using HospitalShared.DTOs;
using System.Net.Http.Json;

namespace OrchestratorService.Infrastructure.HttpClients;

/// <summary>HTTP client for validating patient existence before booking appointments.</summary>
public class PatientServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PatientServiceClient> _logger;

    public PatientServiceClient(HttpClient httpClient, ILogger<PatientServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PatientDto?> GetPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PatientDto>($"api/patients/{patientId}", ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach PatientService for patient {PatientId}", patientId);
            return null;
        }
    }
}
