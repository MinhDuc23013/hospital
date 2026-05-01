using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalShared.Caching;

public static class CacheExtensions
{
    /// <summary>
    /// Registers Redis distributed cache when "Redis:ConnectionString" is configured,
    /// falls back to in-memory distributed cache for local dev without Redis.
    /// </summary>
    public static IServiceCollection AddRedisDistributedCache(
        this IServiceCollection services,
        IConfiguration configuration,
        string? keyPrefix = null)
    {
        var connectionString = configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                if (!string.IsNullOrWhiteSpace(keyPrefix))
                    options.InstanceName = keyPrefix;
            });
        }
        else
        {
            // Fallback: in-process cache (inconsistent across replicas, fine for single-instance dev)
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
