using System.Text.Json;
using HospitalSystem.AiRcaService.Domain;
using Microsoft.Extensions.Caching.Distributed;

namespace HospitalSystem.AiRcaService.Infrastructure.Session;

/// <summary>
/// Redis-backed conversation store using IDistributedCache.
/// Key format: session:{sessionId}. TTL refreshed on every save.
/// Gracefully degrades: Redis failures treated as session-not-found.
/// </summary>
public sealed class RedisConversationStore : IConversationStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisConversationStore> _logger;

    public RedisConversationStore(IDistributedCache cache, ILogger<RedisConversationStore> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<Conversation?> GetAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(BuildKey(sessionId), ct);
            if (bytes is null)
            {
                _logger.LogDebug("Session MISS sessionId={SessionId}", sessionId);
                return null;
            }

            var dto = JsonSerializer.Deserialize<ConversationDto>(bytes, JsonOpts);
            if (dto is null) return null;

            _logger.LogDebug("Session HIT sessionId={SessionId} messages={Count}",
                sessionId, dto.Messages.Count);

            return Conversation.Restore(
                dto.SessionId,
                dto.LastActivity,
                dto.Messages.Select(m => new Message(m.Role, m.Content, m.Timestamp)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for sessionId={SessionId} — treating as miss", sessionId);
            return null;
        }
    }

    public async Task SaveAsync(Conversation conversation, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var dto = new ConversationDto
            {
                SessionId    = conversation.SessionId,
                LastActivity = conversation.LastActivity,
                Messages     = conversation.Messages.Select(m => new MessageDto
                {
                    Role      = m.Role,
                    Content   = m.Content,
                    Timestamp = m.Timestamp
                }).ToList()
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOpts);
            var opts  = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
            await _cache.SetAsync(BuildKey(conversation.SessionId), bytes, opts, ct);

            _logger.LogDebug("Session SAVE sessionId={SessionId} ttl={Ttl}", conversation.SessionId, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for sessionId={SessionId} — session not persisted",
                conversation.SessionId);
        }
    }

    private static string BuildKey(string sessionId) => $"session:{sessionId}";

    // ── DTO types for JSON serialization ─────────────────────────────────────────
    private sealed class ConversationDto
    {
        public string SessionId { get; set; } = string.Empty;
        public List<MessageDto> Messages { get; set; } = [];
        public DateTime LastActivity { get; set; }
    }

    private sealed class MessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
