namespace HospitalSystem.AiRcaService.Domain;

/// <summary>A single message in a conversation (user or assistant turn).</summary>
public sealed record Message(string Role, string Content, DateTime Timestamp);

/// <summary>
/// Session-scoped conversation holding the full multi-turn history for follow-up Q&amp;A.
/// Max turns capped at 10 user+assistant pairs (20 messages) to avoid token overflow.
/// </summary>
public sealed class Conversation
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");
    public List<Message> Messages { get; } = [];
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    /// <summary>Appends a message and refreshes last-activity timestamp.</summary>
    public void AddMessage(string role, string content)
    {
        Messages.Add(new Message(role, content, DateTime.UtcNow));
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the last N user+assistant pairs (maxTurns * 2 messages).
    /// Drops oldest pairs first to stay within token budget.
    /// </summary>
    public IReadOnlyList<Message> GetTrimmedHistory(int maxTurns = 10)
        => Messages.TakeLast(maxTurns * 2).ToList();

    /// <summary>
    /// Reconstructs a Conversation from persisted data (e.g. deserialized from Redis).
    /// Bypasses the auto-generated SessionId so the original ID is preserved.
    /// </summary>
    public static Conversation Restore(string sessionId, DateTime lastActivity, IEnumerable<Message> messages)
    {
        var conv = new Conversation { SessionId = sessionId, LastActivity = lastActivity };
        conv.Messages.AddRange(messages);
        return conv;
    }
}
