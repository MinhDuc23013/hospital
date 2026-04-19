using HospitalShared.Auth;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace HospitalGateway.Extensions;

/// <summary>Extension methods for registering gateway services.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddKeycloakAuth(config);
        return services;
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global: 100 req/s via TokenBucket — smooth rate limiting
            // Refills 100 tokens/second, bucket holds up to 200 for short bursts
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetTokenBucketLimiter("global", _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,           // max burst capacity
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = 100,      // refill 100 tokens/second
                    QueueLimit = 20,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"error":{"message":"Rate limit exceeded. Max 100 requests/second.","code":"RATE_LIMITED"}}""", ct);
            };
        });
        return services;
    }
}
