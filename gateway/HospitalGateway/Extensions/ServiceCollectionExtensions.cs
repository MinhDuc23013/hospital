using HospitalShared.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// Cache validated JWT claims in IMemoryCache so RSA signature verification
    /// only runs once per unique token (up to 60 s TTL or token expiry).
    /// Uses PostConfigure to chain onto the existing JwtBearerEvents without
    /// touching shared KeycloakAuthExtensions.
    /// </summary>
    public static IServiceCollection AddJwtClaimCache(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var originalOnMessageReceived = options.Events?.OnMessageReceived;
            var originalOnTokenValidated  = options.Events?.OnTokenValidated;

            options.Events ??= new JwtBearerEvents();

            // Check cache BEFORE RSA validation — short-circuits the expensive path
            options.Events.OnMessageReceived = async ctx =>
            {
                if (originalOnMessageReceived != null)
                    await originalOnMessageReceived(ctx);

                var token = ExtractBearerToken(ctx.Request);
                if (token is null) return;

                var cache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                if (cache.TryGetValue(BuildCacheKey(token), out ClaimsPrincipal? principal) && principal is not null)
                {
                    ctx.Principal = principal;
                    ctx.Success(); // skip JWT validation entirely
                }
            };

            // Store validated principal — TTL = min(remaining token lifetime, 60 s)
            options.Events.OnTokenValidated = async ctx =>
            {
                if (originalOnTokenValidated != null)
                    await originalOnTokenValidated(ctx);

                if (ctx.Principal is null) return;

                var token = ExtractBearerToken(ctx.Request);
                if (token is null) return;

                var ttl = ctx.SecurityToken is JwtSecurityToken jwt
                    ? TimeSpan.FromSeconds(Math.Min(Math.Max((jwt.ValidTo - DateTime.UtcNow).TotalSeconds, 0), 60))
                    : TimeSpan.FromSeconds(30);

                if (ttl > TimeSpan.Zero)
                {
                    var cache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                    cache.Set(BuildCacheKey(token), ctx.Principal, ttl);
                }
            };
        });

        return services;
    }

    private static string? ExtractBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : null;
    }

    // First 16 hex chars of SHA-256 (64-bit prefix) — negligible collision risk for a local cache
    private static string BuildCacheKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return $"jwt:{Convert.ToHexString(hash)[..16]}";
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global: 1000 req/s via TokenBucket — burst up to 2000
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetTokenBucketLimiter("global", _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 8000,          // max burst capacity
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = 4000,     // refill 4000 tokens/second
                    QueueLimit = 100,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"error":{"message":"Rate limit exceeded. Max 4000 requests/second.","code":"RATE_LIMITED"}}""", ct);
            };
        });
        return services;
    }
}
