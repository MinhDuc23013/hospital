using HospitalShared.DTOs;
using MediatR;
using PatientService.Application.Queries;
using PatientService.Infrastructure.Repositories;
using ZiggyCreatures.Caching.Fusion;

namespace PatientService.Application.Handlers;

public class GetPatientHandler : IRequestHandler<GetPatientQuery, PatientDto?>
{
    private readonly IPatientRepository _repo;
    private readonly IFusionCache _cache;
    private readonly ILogger<GetPatientHandler> _logger;

    private static readonly FusionCacheEntryOptions CacheOptions =
        new() { Duration = TimeSpan.FromMinutes(5), Size = 1 };
    private static readonly FusionCacheEntryOptions NotFoundOptions =
        new() { Duration = TimeSpan.FromSeconds(30), Size = 1 };
    private static readonly FusionCacheEntryOptions L1ProbeOptions =
        new() { Duration = TimeSpan.FromMinutes(5), Size = 1, SkipDistributedCacheRead = true, SkipDistributedCacheWrite = true };

    public GetPatientHandler(IPatientRepository repo, IFusionCache cache, ILogger<GetPatientHandler> logger)
    {
        _repo   = repo;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<PatientDto?> Handle(GetPatientQuery query, CancellationToken ct)
    {
        var cacheKey = $"patient:{query.Id}";

        var l1 = await _cache.TryGetAsync<PatientDto?>(cacheKey, options: L1ProbeOptions, token: ct);
        if (l1.HasValue)
        {
            _logger.LogInformation(
                "Patient resolved from L1 local cache. PatientId={PatientId} CacheKey={CacheKey} CacheResult={CacheResult} CacheLayer={CacheLayer} DataSource={DataSource}",
                query.Id, cacheKey, "hit", "local", "cache");
            return l1.Value;
        }

        var l2 = await _cache.TryGetAsync<PatientDto?>(cacheKey, token: ct);
        if (l2.HasValue)
        {
            _logger.LogInformation(
                "Patient resolved from L2 distributed cache (Redis). PatientId={PatientId} CacheKey={CacheKey} CacheResult={CacheResult} CacheLayer={CacheLayer} DataSource={DataSource}",
                query.Id, cacheKey, "hit", "distributed", "cache");
            return l2.Value;
        }

        _logger.LogInformation(
            "Patient not in cache, fetching from DB. PatientId={PatientId} CacheKey={CacheKey} CacheResult={CacheResult} CacheLayer={CacheLayer} DataSource={DataSource}",
            query.Id, cacheKey, "miss", "none", "db");

        var patient = await _repo.GetByIdAsync(query.Id, ct);
        var dto = patient is null ? null : CreatePatientHandler.MapToDto(patient);
        await _cache.SetAsync(cacheKey, dto, dto != null ? CacheOptions : NotFoundOptions, ct);
        return dto;
    }
}
