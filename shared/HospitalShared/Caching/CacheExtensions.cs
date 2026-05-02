using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace HospitalShared.Caching;

public static class CacheExtensions
{
    /// <summary>
    /// L1 (local memory) + L2 (Redis) + Backplane (Redis pub/sub cross-node invalidation).
    /// Falls back to L1-only when Redis is not configured.
    /// </summary>
    /// <param name="instanceName">Key prefix in Redis, also scopes the backplane channel.</param>
    /// <param name="maxEntries">Max entries in L1 memory cache per node (default 2000 ≈ ~1-2MB).</param>
    public static IServiceCollection AddPatientFusionCache(
        this IServiceCollection services,
        IConfiguration configuration,
        string instanceName,
        int maxEntries = 2000)
    {
        var connectionString = configuration["Redis:ConnectionString"];

        var builder = services
            .AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5),
                // Size = 1 means each entry occupies 1 slot; SizeLimit=maxEntries caps total entries
                Size = 1,
            })
            .WithMemoryCache(new MemoryCache(new MemoryCacheOptions { SizeLimit = maxEntries }))
            .WithSystemTextJsonSerializer();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // L2: shared Redis cache (survives restarts, shared across deploys)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = instanceName;
            });

            // Backplane: Redis pub/sub — when any node evicts a key, all other nodes evict too
            builder
                .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
                .WithStackExchangeRedisBackplane(opt => opt.Configuration = connectionString);
        }

        return services;
    }
}
