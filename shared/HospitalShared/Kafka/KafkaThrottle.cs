using Microsoft.Extensions.Logging;

namespace HospitalShared.Kafka;

/// <summary>
/// Token-bucket throttle for Kafka consumers.
///
/// Each call to <see cref="WaitAsync"/> consumes one token.
/// Tokens are replenished up to <see cref="MaxPerSecond"/> every second,
/// so the consumer naturally slows to at most N messages/s — back-pressure
/// propagates as growing consumer lag visible in Grafana / Prometheus.
/// </summary>
public sealed class KafkaThrottle : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _refillTimer;
    private readonly ILogger<KafkaThrottle>? _logger;

    public int MaxPerSecond { get; }

    /// <param name="maxMessagesPerSecond">Max messages processed per second. Must be ≥ 1.</param>
    public KafkaThrottle(int maxMessagesPerSecond, ILogger<KafkaThrottle>? logger = null)
    {
        if (maxMessagesPerSecond < 1)
            throw new ArgumentOutOfRangeException(nameof(maxMessagesPerSecond), "Must be ≥ 1");

        MaxPerSecond = maxMessagesPerSecond;
        _logger = logger;

        // SemaphoreSlim acts as the token bucket — starts full
        _semaphore = new SemaphoreSlim(maxMessagesPerSecond, maxMessagesPerSecond);

        // Refill tokens every second up to the bucket ceiling
        _refillTimer = new Timer(_ => Refill(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Waits until a token is available, then consumes it.
    /// Blocks the consumer's poll loop when the rate limit is hit — this is the throttle.
    /// </summary>
    public Task WaitAsync(CancellationToken ct) => _semaphore.WaitAsync(ct);

    private void Refill()
    {
        // How many tokens are missing from the bucket?
        var missing = MaxPerSecond - _semaphore.CurrentCount;
        if (missing <= 0) return;

        _semaphore.Release(missing);
        _logger?.LogDebug("KafkaThrottle: refilled {Count} tokens (max={Max}/s)", missing, MaxPerSecond);
    }

    public void Dispose()
    {
        _refillTimer.Dispose();
        _semaphore.Dispose();
    }
}
