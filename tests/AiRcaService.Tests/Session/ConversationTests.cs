using FluentAssertions;
using HospitalSystem.AiRcaService.Domain;
using Xunit;

namespace AiRcaService.Tests.Session;

public sealed class ConversationTests
{
    // New conversation has a non-empty SessionId and no messages
    [Fact]
    public void NewConversation_HasSessionIdAndEmptyMessages()
    {
        var conv = new Conversation();

        conv.SessionId.Should().NotBeNullOrWhiteSpace();
        conv.Messages.Should().BeEmpty();
        conv.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // AddMessage appends to Messages and updates LastActivity
    [Fact]
    public void AddMessage_AppendsMessageAndUpdatesLastActivity()
    {
        var conv   = new Conversation();
        var before = DateTime.UtcNow;

        conv.AddMessage("user", "Hello");
        conv.AddMessage("assistant", "Hi there");

        conv.Messages.Should().HaveCount(2);
        conv.Messages[0].Role.Should().Be("user");
        conv.Messages[0].Content.Should().Be("Hello");
        conv.Messages[1].Role.Should().Be("assistant");
        conv.Messages[1].Content.Should().Be("Hi there");
        conv.LastActivity.Should().BeOnOrAfter(before);
    }

    // GetTrimmedHistory returns all messages when under the cap
    [Fact]
    public void GetTrimmedHistory_UnderCap_ReturnsAllMessages()
    {
        var conv = new Conversation();
        for (var i = 0; i < 6; i++)
        {
            conv.AddMessage(i % 2 == 0 ? "user" : "assistant", $"message {i}");
        }

        var history = conv.GetTrimmedHistory(maxTurns: 10);

        history.Should().HaveCount(6);
    }

    // GetTrimmedHistory truncates to maxTurns * 2 messages (oldest dropped)
    [Fact]
    public void GetTrimmedHistory_OverCap_TruncatesOldestFirst()
    {
        var conv = new Conversation();
        // 12 messages = 6 turns → with maxTurns=4 (8 messages), oldest 4 should be dropped
        for (var i = 0; i < 12; i++)
        {
            conv.AddMessage(i % 2 == 0 ? "user" : "assistant", $"msg-{i}");
        }

        var history = conv.GetTrimmedHistory(maxTurns: 4);

        history.Should().HaveCount(8);
        history[0].Content.Should().Be("msg-4"); // oldest kept
        history[^1].Content.Should().Be("msg-11"); // newest
    }

    // Restore re-creates conversation with the correct SessionId and messages
    [Fact]
    public void Restore_RecreatesConversationWithCorrectData()
    {
        var sessionId    = "abc123";
        var lastActivity = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var messages     = new[]
        {
            new Message("user",      "What happened?",  lastActivity.AddMinutes(-5)),
            new Message("assistant", "DB pool timeout", lastActivity.AddMinutes(-4))
        };

        var conv = Conversation.Restore(sessionId, lastActivity, messages);

        conv.SessionId.Should().Be(sessionId);
        conv.LastActivity.Should().Be(lastActivity);
        conv.Messages.Should().HaveCount(2);
        conv.Messages[0].Role.Should().Be("user");
        conv.Messages[1].Role.Should().Be("assistant");
    }
}
