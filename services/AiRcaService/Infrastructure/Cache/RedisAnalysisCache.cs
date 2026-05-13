using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace HospitalSystem.AiRcaService.Infrastructure.Cache;

/// <summary>
/// Redis-backed analysis cache using IDistributedCache (StackExchange.Redis).
/// Gracefully degrades: if Redis is down, logs warning and treats as cache miss.
/// </summary>
public sealed class RedisAnalysisCache : IAnalysisCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisAnalysisCache> _logger;

    public RedisAnalysisCache(IDistributedCache cache, ILogger<RedisAnalysisCache> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<string?> GetHtmlAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null)
            {
                _logger.LogDebug("Cache MISS key={Key}", key);
                return null;
            }

            _logger.LogDebug("Cache HIT key={Key}", key);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key={Key} — treating as cache miss", key);
            return null;
        }
    }

    public async Task SetHtmlAsync(string key, string html, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var bytes   = Encoding.UTF8.GetBytes(html);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
            await _cache.SetAsync(key, bytes, options, ct);
            _logger.LogDebug("Cache SET key={Key} ttl={Ttl}", key, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key={Key} — result not cached", key);
        }
    }

    /// <summary>
    /// Generates a deterministic, short cache key from analysis parameters.
    /// Format: airca:{first-16-chars-of-sha256-hex}
    /// </summary>
    public static string BuildKey(string service, DateTime from, DateTime to, string? traceId)
    {
        var raw  = $"{service}:{from.Ticks}:{to.Ticks}:{traceId ?? ""}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return "airca:" + Convert.ToHexString(hash)[..16].ToLower();
    }
}
