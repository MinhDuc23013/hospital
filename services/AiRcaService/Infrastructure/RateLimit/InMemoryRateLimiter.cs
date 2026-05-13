using System.Collections.Concurrent;

namespace HospitalSystem.AiRcaService.Infrastructure.RateLimit;

/// <summary>
/// Sliding-window in-memory rate limiter.
/// Default: 10 requests per hour per client ID (IP address).
/// Uses ConcurrentDictionary + Queue for thread safety without locks.
/// </summary>
public sealed class InMemoryRateLimiter : IRateLimiter
{
    private readonly int _requestsPerHour;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new();

    public InMemoryRateLimiter(IConfiguration config)
    {
        _requestsPerHour = config.GetValue<int>("RateLimit:RequestsPerHour", 10);
    }

    public bool TryAcquire(string clientId)
    {
        var now    = DateTime.UtcNow;
        var window = _windows.GetOrAdd(clientId, _ => new Queue<DateTime>());

        lock (window)
        {
            // Evict timestamps older than 1 hour
            var cutoff = now.AddHours(-1);
            while (window.Count > 0 && window.Peek() < cutoff)
                window.Dequeue();

            if (window.Count >= _requestsPerHour)
                return false;

            window.Enqueue(now);
            return true;
        }
    }
}
