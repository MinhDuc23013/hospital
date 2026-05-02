using HospitalShared.DTOs;
using System.Net.Http.Json;
using ZiggyCreatures.Caching.Fusion;

namespace OrchestratorService.Infrastructure.HttpClients;

/// <summary>HTTP client for validating patient existence before booking appointments.</summary>
public class PatientServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IFusionCache _cache;
    private readonly ILogger<PatientServiceClient> _logger;

    private static readonly FusionCacheEntryOptions CacheOptions =
        new() { Duration = TimeSpan.FromMinutes(5), Size = 1 };
    private static readonly FusionCacheEntryOptions NotFoundOptions =
        new() { Duration = TimeSpan.FromSeconds(30), Size = 1 };
    // L1-only probe: skip distributed (Redis) read+write so we only check in-memory cache
    private static readonly FusionCacheEntryOptions L1ProbeOptions =
        new() { Duration = TimeSpan.FromMinutes(5), Size = 1, SkipDistributedCacheRead = true, SkipDistributedCacheWrite = true };

    public PatientServiceClient(HttpClient httpClient, IFusionCache cache, ILogger<PatientServiceClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PatientDto?> GetPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var cacheKey = $"patient:{patientId}";

        // L1 probe: check in-memory cache only (nanosecond, no network)
        var l1 = await _cache.TryGetAsync<PatientDto?>(cacheKey, options: L1ProbeOptions, token: ct);
        if (l1.HasValue)
        {
            _logger.LogInformation(
                "Patient resolved from L1 local cache. PatientId={PatientId} CacheKey={CacheKey} CacheResult={CacheResult} CacheLayer={CacheLayer} DataSource={DataSource}",
                patientId, cacheKey, "hit", "local", "cache");
            return l1.Value;
        }

        // L2 probe: L1 missed, check distributed Redis cache
        var l2 = await _cache.TryGetAsync<PatientDto?>(cacheKey, token: ct);
        if (l2.HasValue)
        {
            _logger.LogInformation(
                "Patient resolved from L2 distributed cache (Redis). PatientId={PatientId} CacheKey={CacheKey} CacheResult={CacheResult} CacheLayer={CacheLayer} DataSource={DataSource}",
                patientId, cacheKey, "hit", "distributed", "cache");
            return l2.Value;
        }

        _logger.LogInformation(
            "Patient not in cache, fetching from PatientService. PatientId={PatientId} CacheKey={CacheKey} CacheResult={CacheResult} CacheLayer={CacheLayer} DataSource={DataSource}",
            patientId, cacheKey, "miss", "none", "http");

        try
        {
            var patient = await _httpClient.GetFromJsonAsync<PatientDto>($"api/patients/{patientId}", ct);
            // null = not found → negative cache 30s to avoid hammering PatientService
            await _cache.SetAsync(cacheKey, patient, patient != null ? CacheOptions : NotFoundOptions, ct);
            return patient;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach PatientService for patient {PatientId}", patientId);
            return null;
        }
    }
}
