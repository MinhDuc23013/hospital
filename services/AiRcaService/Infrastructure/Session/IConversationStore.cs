using HospitalSystem.AiRcaService.Domain;

namespace HospitalSystem.AiRcaService.Infrastructure.Session;

/// <summary>Persistent store for multi-turn conversation sessions (Redis-backed).</summary>
public interface IConversationStore
{
    /// <summary>Loads a conversation by session ID. Returns null if expired or not found.</summary>
    Task<Conversation?> GetAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Persists conversation with the given TTL (refreshed on each save).</summary>
    Task SaveAsync(Conversation conversation, TimeSpan ttl, CancellationToken ct = default);
}
