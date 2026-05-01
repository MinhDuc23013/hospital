using HospitalShared.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Json;
using System.Text.Json;

namespace AppointmentService.Infrastructure.HttpClients;

/// <summary>HTTP client for validating patient existence before scheduling appointments.</summary>
public class PatientServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PatientServiceClient> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NotFoundTtl = TimeSpan.FromSeconds(30);
    private const string NotFoundSentinel = "__NOT_FOUND__";

    public PatientServiceClient(HttpClient httpClient, IDistributedCache cache, ILogger<PatientServiceClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PatientDto?> GetPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var cacheKey = $"patient:{patientId}";

        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for patient {PatientId}", patientId);
            return cached == NotFoundSentinel ? null : JsonSerializer.Deserialize<PatientDto>(cached);
        }

        try
        {
            var patient = await _httpClient.GetFromJsonAsync<PatientDto>($"api/patients/{patientId}", ct);
            if (patient != null)
            {
                var json = JsonSerializer.Serialize(patient);
                await _cache.SetStringAsync(cacheKey, json,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);
            }
            else
            {
                // Negative cache: avoid hammering PatientService for non-existent patients
                await _cache.SetStringAsync(cacheKey, NotFoundSentinel,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = NotFoundTtl }, ct);
            }
            return patient;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach PatientService for patient {PatientId}", patientId);
            return null;
        }
    }
}
