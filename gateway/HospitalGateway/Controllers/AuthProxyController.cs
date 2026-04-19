using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace HospitalGateway.Controllers;

/// <summary>
/// Proxies Keycloak token endpoint with sliding-window brute force protection.
/// Limit: 5 failed attempts per username in any 10-minute window.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthProxyController : ControllerBase
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthProxyController> _logger;

    public AuthProxyController(
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AuthProxyController> logger)
    {
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token(CancellationToken ct)
    {
        // Parse form data (Keycloak token endpoint uses application/x-www-form-urlencoded)
        var form = await Request.ReadFormAsync(ct);
        var username = form["username"].ToString();
        var grantType = form["grant_type"].ToString();

        // Only apply brute-force check on password grant type
        if (grantType == "password" && !string.IsNullOrEmpty(username))
        {
            var db = _redis.GetDatabase();
            var key = $"login_failures:{username.ToLowerInvariant()}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (long)Window.TotalMilliseconds;

            // 1. Remove entries older than 10 minutes
            await db.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, windowStart);

            // 2. Count failures in current sliding window
            var failureCount = await db.SortedSetLengthAsync(key);

            if (failureCount >= MaxFailures)
            {
                // Find oldest failure to calculate retry-after
                var oldest = await db.SortedSetRangeByRankWithScoresAsync(key, 0, 0);
                var retryAfter = oldest.Length > 0
                    ? (int)Math.Ceiling((oldest[0].Score + Window.TotalMilliseconds - now) / 1000.0)
                    : 600;

                _logger.LogWarning(
                    "Brute force protection triggered: user={Username}, failures={Count}, retryAfter={Seconds}s",
                    username, failureCount, retryAfter);

                Response.Headers["Retry-After"] = retryAfter.ToString();
                return StatusCode(429, new
                {
                    error = new
                    {
                        code = "TOO_MANY_LOGIN_ATTEMPTS",
                        message = $"Too many failed login attempts. Try again in {retryAfter} seconds.",
                        retryAfter
                    }
                });
            }
        }

        // Forward request to Keycloak
        var keycloakUrl = _config["Keycloak:Authority"] ?? "http://keycloak:8080/realms/hospital";
        var tokenEndpoint = $"{keycloakUrl}/protocol/openid-connect/token";

        using var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(
            form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()));

        var keycloakResponse = await client.PostAsync(tokenEndpoint, content, ct);
        var responseBody = await keycloakResponse.Content.ReadAsStringAsync(ct);

        // Track failures for password grant
        if (grantType == "password" && !string.IsNullOrEmpty(username))
        {
            var db = _redis.GetDatabase();
            var key = $"login_failures:{username.ToLowerInvariant()}";

            if (keycloakResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Wrong password — add timestamp to sorted set
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await db.SortedSetAddAsync(key, now.ToString(), now);
                await db.KeyExpireAsync(key, Window); // auto-cleanup

                _logger.LogWarning("Failed login attempt: user={Username}, ip={Ip}",
                    username, HttpContext.Connection.RemoteIpAddress);
            }
            else if (keycloakResponse.IsSuccessStatusCode)
            {
                // Successful login — clear failure counter
                await db.KeyDeleteAsync(key);
            }
        }

        // Pass through Keycloak raw JSON (avoid re-serialization wrapping it in quotes)
        var contentType = keycloakResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
        return new ContentResult
        {
            Content = responseBody,
            ContentType = contentType,
            StatusCode = (int)keycloakResponse.StatusCode,
        };
    }
}
