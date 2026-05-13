using FluentAssertions;
using HospitalSystem.AiRcaService.Application.Services;
using HospitalSystem.AiRcaService.Infrastructure.Cache;
using HospitalSystem.AiRcaService.Infrastructure.Jaeger;
using HospitalSystem.AiRcaService.Infrastructure.Llm;
using HospitalSystem.AiRcaService.Infrastructure.Loki;
using HospitalSystem.AiRcaService.Infrastructure.Redaction;
using HospitalSystem.AiRcaService.Infrastructure.Rendering;
using HospitalSystem.AiRcaService.Infrastructure.Session;
using HospitalSystem.AiRcaService.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiRcaService.Tests.Application;

public sealed class LogAnalysisServiceTests
{
    private readonly Mock<ILokiClient>          _lokiClient        = new();
    private readonly Mock<ILlmProvider>         _llmProvider       = new();
    private readonly Mock<IHtmlRenderer>        _htmlRenderer      = new();
    private readonly Mock<IPiiRedactor>         _piiRedactor       = new();
    private readonly Mock<IAnalysisCache>       _cache             = new();
    private readonly Mock<IJaegerClient>        _jaegerClient      = new();
    private readonly Mock<IConversationStore>   _conversationStore = new();
    private readonly IConfiguration             _config;

    private static readonly DateTime From = new(2024, 6, 1, 8,  0, 0, DateTimeKind.Utc);
    private static readonly DateTime To   = new(2024, 6, 1, 8, 30, 0, DateTimeKind.Utc);

    public LogAnalysisServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analysis:MaxLogLines"]      = "5000",
                ["Analysis:MaxWindowMinutes"] = "60"
            })
            .Build();

        // Default: cache miss, Jaeger returns null
        _cache.Setup(c => c.GetHtmlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((string?)null);
        _cache.Setup(c => c.SetHtmlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _jaegerClient.Setup(j => j.FetchTraceSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((string?)null);
        // Default: conversation store succeeds silently
        _conversationStore.Setup(s => s.SaveAsync(It.IsAny<Conversation>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);
    }

    private LogAnalysisService CreateSut() => new(
        _lokiClient.Object,
        _piiRedactor.Object,
        _llmProvider.Object,
        _htmlRenderer.Object,
        _cache.Object,
        _jaegerClient.Object,
        _conversationStore.Object,
        _config,
        NullLogger<LogAnalysisService>.Instance);

    private static LokiLogEntry MakeEntry(string msg) =>
        new(DateTime.UtcNow, "error", msg, new Dictionary<string, string>(), null);

    private static StructuredAnalysis MakeAnalysis() => new(
        RootCause:           "DB connection pool exhausted",
        Evidence:            new[] { new EvidenceItem("DB pool error", "2024-06-01", "pool exhausted") },
        SuggestedFix:        "Increase pool size",
        Confidence:          "High",
        ConfidenceReasoning: "Clear evidence in logs",
        RelatedServices:     Array.Empty<string>());

    // Happy path: full pipeline — cache miss → Loki → LLM → render → cache set
    [Fact]
    public async Task AnalyzeAsync_HappyPath_ReturnHtml()
    {
        var entries  = new[] { MakeEntry("DB connection failed") };
        var redacted = new RedactionResult(entries, 0, new Dictionary<string, int>());
        var analysis = MakeAnalysis();
        var metadata = new RenderMetadata("svc", From, To, 0);

        _lokiClient.Setup(c => c.FetchLogsAsync("svc", From, To, 5000, default))
                   .ReturnsAsync(entries);
        _piiRedactor.Setup(r => r.Redact(entries)).Returns(redacted);
        _llmProvider.Setup(l => l.AnalyzeStructuredAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                    .ReturnsAsync(analysis);
        _htmlRenderer.Setup(h => h.RenderStructured(analysis, It.Is<RenderMetadata>(m => m.Service == "svc"), null))
                     .Returns("<html>result</html>");
        _htmlRenderer.Setup(h => h.InjectSessionId("<html>result</html>", It.IsAny<string>()))
                     .Returns("<html>result-with-session</html>");

        var result = await CreateSut().AnalyzeAsync("svc", From, To, null);

        result.Html.Should().Be("<html>result-with-session</html>");
        result.SessionId.Should().NotBeNullOrWhiteSpace();
        _llmProvider.Verify(l => l.AnalyzeStructuredAsync(
            It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        _cache.Verify(c => c.SetHtmlAsync(
            It.IsAny<string>(), "<html>result</html>", TimeSpan.FromHours(1), default), Times.Once);
    }

    // Cache HIT: returns cached HTML with fresh session — no Loki or LLM calls
    [Fact]
    public async Task AnalyzeAsync_CacheHit_ReturnsCachedHtmlWithoutCallingLlm()
    {
        _cache.Setup(c => c.GetHtmlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync("<html>cached</html>");
        _htmlRenderer.Setup(h => h.InjectSessionId("<html>cached</html>", It.IsAny<string>()))
                     .Returns("<html>cached-with-session</html>");

        var result = await CreateSut().AnalyzeAsync("svc", From, To, null);

        result.Html.Should().Be("<html>cached-with-session</html>");
        result.SessionId.Should().NotBeNullOrWhiteSpace();
        _lokiClient.Verify(c => c.FetchLogsAsync(
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _llmProvider.Verify(l => l.AnalyzeStructuredAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Empty logs → renders "no logs" HTML, no LLM call, no session
    [Fact]
    public async Task AnalyzeAsync_EmptyLogs_RendersNoLogsHtml()
    {
        _lokiClient.Setup(c => c.FetchLogsAsync("svc", From, To, 5000, default))
                   .ReturnsAsync(Array.Empty<LokiLogEntry>());
        _htmlRenderer.Setup(h => h.Render(
            It.Is<string>(s => s.Contains("No Logs Found")), "svc", From, To, 0))
            .Returns("<html>no logs</html>");

        var result = await CreateSut().AnalyzeAsync("svc", From, To, null);

        result.Html.Should().Contain("no logs");
        result.SessionId.Should().BeEmpty();
        _llmProvider.Verify(l => l.AnalyzeStructuredAsync(
            It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    // Validation: empty service
    [Fact]
    public async Task AnalyzeAsync_EmptyService_ThrowsArgumentException()
    {
        await CreateSut().Invoking(s => s.AnalyzeAsync("", From, To, null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Service name*");
    }

    // Validation: from >= to
    [Fact]
    public async Task AnalyzeAsync_FromAfterTo_ThrowsArgumentException()
    {
        await CreateSut().Invoking(s => s.AnalyzeAsync("svc", To, From, null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*from*");
    }

    // Validation: window > 60 minutes
    [Fact]
    public async Task AnalyzeAsync_WindowExceedsMax_ThrowsArgumentException()
    {
        var bigTo = From.AddHours(2);
        await CreateSut().Invoking(s => s.AnalyzeAsync("svc", From, bigTo, null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*60*");
    }

    // Loki failure propagates
    [Fact]
    public async Task AnalyzeAsync_LokiThrows_PropagatesException()
    {
        _lokiClient.Setup(c => c.FetchLogsAsync("svc", From, To, 5000, default))
                   .ThrowsAsync(new HttpRequestException("Loki unavailable"));

        await CreateSut().Invoking(s => s.AnalyzeAsync("svc", From, To, null))
            .Should().ThrowAsync<HttpRequestException>();
    }

    // LLM failure propagates
    [Fact]
    public async Task AnalyzeAsync_LlmThrows_PropagatesException()
    {
        var entries  = new[] { MakeEntry("error log") };
        var redacted = new RedactionResult(entries, 0, new Dictionary<string, int>());

        _lokiClient.Setup(c => c.FetchLogsAsync("svc", From, To, 5000, default))
                   .ReturnsAsync(entries);
        _piiRedactor.Setup(r => r.Redact(entries)).Returns(redacted);
        _llmProvider.Setup(l => l.AnalyzeStructuredAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                    .ThrowsAsync(new HttpRequestException("Anthropic 503"));

        await CreateSut().Invoking(s => s.AnalyzeAsync("svc", From, To, null))
            .Should().ThrowAsync<HttpRequestException>();
    }

    // Jaeger enrichment: traceId present → FetchTraceSummaryAsync called
    [Fact]
    public async Task AnalyzeAsync_WithTraceId_CallsJaegerClient()
    {
        var entries  = new[] { MakeEntry("error") };
        var redacted = new RedactionResult(entries, 0, new Dictionary<string, int>());
        var analysis = MakeAnalysis();

        _lokiClient.Setup(c => c.FetchLogsAsync("svc", From, To, 5000, default))
                   .ReturnsAsync(entries);
        _piiRedactor.Setup(r => r.Redact(entries)).Returns(redacted);
        _jaegerClient.Setup(j => j.FetchTraceSummaryAsync("trace-001", default))
                     .ReturnsAsync("TRACE SUMMARY ...");
        _llmProvider.Setup(l => l.AnalyzeStructuredAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("JAEGER TRACE")),
            default))
            .ReturnsAsync(analysis);
        _htmlRenderer.Setup(h => h.RenderStructured(It.IsAny<StructuredAnalysis>(), It.IsAny<RenderMetadata>(), null))
                     .Returns("<html>with-trace</html>");
        _htmlRenderer.Setup(h => h.InjectSessionId("<html>with-trace</html>", It.IsAny<string>()))
                     .Returns("<html>with-trace-session</html>");

        var result = await CreateSut().AnalyzeAsync("svc", From, To, "trace-001");

        result.Html.Should().Be("<html>with-trace-session</html>");
        _jaegerClient.Verify(j => j.FetchTraceSummaryAsync("trace-001", default), Times.Once);
    }
}
