using FluentAssertions;
using HospitalSystem.AiRcaService.Controllers;
using HospitalSystem.AiRcaService.Domain;
using HospitalSystem.AiRcaService.Infrastructure.Llm;
using HospitalSystem.AiRcaService.Infrastructure.RateLimit;
using HospitalSystem.AiRcaService.Infrastructure.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using Xunit;

namespace AiRcaService.Tests.Controllers;

public sealed class FollowupControllerTests
{
    private readonly Mock<IConversationStore> _store      = new();
    private readonly Mock<ILlmProvider>       _llm        = new();
    private readonly Mock<IRateLimiter>       _rateLimiter = new();

    private FollowupController CreateSut()
    {
        var ctrl = new FollowupController(
            _store.Object,
            _llm.Object,
            _rateLimiter.Object,
            NullLogger<FollowupController>.Instance);

        // Provide a minimal HttpContext with a remote IP
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = IPAddress.Loopback }
            }
        };

        return ctrl;
    }

    private static Conversation MakeConversation(string sessionId = "sess001")
    {
        var conv = Conversation.Restore(sessionId, DateTime.UtcNow, new[]
        {
            new Message("user",      "initial prompt", DateTime.UtcNow.AddMinutes(-5)),
            new Message("assistant", "root cause: DB pool exhausted", DateTime.UtcNow.AddMinutes(-4))
        });
        return conv;
    }

    // Session not found → returns 200 with error HTML fragment (HTMX expects 200 for swap)
    [Fact]
    public async Task FollowupAsync_SessionNotFound_ReturnsErrorFragment()
    {
        _rateLimiter.Setup(r => r.TryAcquire(It.IsAny<string>())).Returns(true);
        _store.Setup(s => s.GetAsync("missing-id", It.IsAny<CancellationToken>()))
              .ReturnsAsync((Conversation?)null);

        var result = await CreateSut().FollowupAsync("missing-id", "What happened?", default);

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("text/html");
        content.Content.Should().Contain("Session expired");
    }

    // Happy path: returns HTML fragment with user bubble + assistant bubble
    [Fact]
    public async Task FollowupAsync_ValidSession_ReturnsHtmlFragment()
    {
        var conv = MakeConversation("sess001");
        _rateLimiter.Setup(r => r.TryAcquire(It.IsAny<string>())).Returns(true);
        _store.Setup(s => s.GetAsync("sess001", It.IsAny<CancellationToken>()))
              .ReturnsAsync(conv);
        _store.Setup(s => s.SaveAsync(It.IsAny<Conversation>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _llm.Setup(l => l.AnalyzeMultiTurnAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse("The issue is **X**.", 100, 50, "claude-sonnet-4-6"));

        var result = await CreateSut().FollowupAsync("sess001", "Any other errors?", default);

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("text/html");
        content.Content.Should().Contain("user-message");
        content.Content.Should().Contain("assistant-message");
        content.Content.Should().Contain("Any other errors?");
        content.Content.Should().Contain("issue is");
    }

    // Rate limit exceeded → returns error fragment (not 429 JSON — HTMX needs HTML)
    [Fact]
    public async Task FollowupAsync_RateLimitExceeded_ReturnsErrorFragment()
    {
        _rateLimiter.Setup(r => r.TryAcquire(It.IsAny<string>())).Returns(false);

        var result = await CreateSut().FollowupAsync("sess001", "Any other errors?", default);

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("text/html");
        content.Content.Should().Contain("Rate limit exceeded");
    }

    // LLM failure → returns graceful error fragment, session still valid
    [Fact]
    public async Task FollowupAsync_LlmThrows_ReturnsErrorFragment()
    {
        var conv = MakeConversation("sess002");
        _rateLimiter.Setup(r => r.TryAcquire(It.IsAny<string>())).Returns(true);
        _store.Setup(s => s.GetAsync("sess002", It.IsAny<CancellationToken>()))
              .ReturnsAsync(conv);
        _llm.Setup(l => l.AnalyzeMultiTurnAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<LlmMessage>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Anthropic 503"));

        var result = await CreateSut().FollowupAsync("sess002", "Tell me more.", default);

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("text/html");
        content.Content.Should().Contain("temporarily unavailable");
        // Session save must NOT be called on LLM failure
        _store.Verify(s => s.SaveAsync(
            It.IsAny<Conversation>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
