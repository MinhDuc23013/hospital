using System.Security.Cryptography;
using System.Text.Json;
using StackExchange.Redis;

namespace HospitalGateway.Bff;

/// <summary>
/// Stores BFF sessions in Redis — browser holds only a random session ID cookie,
/// actual tokens never leave the server.
/// </summary>
public class BffSessionService
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(8);

    public BffSessionService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<string> CreateAsync(BffSession session, CancellationToken ct = default)
    {
        var sessionId = GenerateSessionId();
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"bff:{sessionId}", JsonSerializer.Serialize(session), SessionTtl);
        return sessionId;
    }

    public async Task<BffSession?> GetAsync(string sessionId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"bff:{sessionId}");
        return value.HasValue ? JsonSerializer.Deserialize<BffSession>(value!) : null;
    }

    public async Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"bff:{sessionId}");
    }

    public async Task UpdateTokensAsync(
        string sessionId, string accessToken, string refreshToken, long expiresAt, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var existing = await GetAsync(sessionId, ct);
        if (existing is null) return;

        var updated = existing with { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresAt = expiresAt };

        // Preserve remaining TTL so session doesn't get extended on every refresh
        var remaining = await db.KeyTimeToLiveAsync($"bff:{sessionId}");
        await db.StringSetAsync($"bff:{sessionId}", JsonSerializer.Serialize(updated), remaining ?? SessionTtl);
    }

    private static string GenerateSessionId()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
