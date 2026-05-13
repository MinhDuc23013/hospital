namespace HospitalSystem.AiRcaService.Infrastructure.RateLimit;

/// <summary>Sliding-window rate limiter per client identifier (e.g. IP address).</summary>
public interface IRateLimiter
{
    /// <summary>
    /// Returns true if the request is allowed; false if the limit is exceeded.
    /// </summary>
    bool TryAcquire(string clientId);
}
