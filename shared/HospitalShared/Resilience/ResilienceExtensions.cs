using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace HospitalShared.Resilience;

/// <summary>
/// Adds standard HTTP resilience (retry + circuit breaker + timeout) to any HttpClient.
///
/// Retry:           3 attempts, exponential backoff (2s → 4s → 8s)
/// Circuit breaker: opens after 50% failure rate over 10 requests in 30s window; stays open for 30s
/// Attempt timeout: 10s per attempt
/// Total timeout:   30s across all attempts
///
/// Usage: builder.Services.AddHttpClient&lt;MyClient&gt;(...).AddResilienceHandler();
/// </summary>
public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.UseJitter = true;

            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });
        return builder;
    }
}
