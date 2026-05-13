using HospitalSystem.AiRcaService.Domain;
using HospitalSystem.AiRcaService.Infrastructure.Cache;
using HospitalSystem.AiRcaService.Infrastructure.Jaeger;
using HospitalSystem.AiRcaService.Infrastructure.Llm;
using HospitalSystem.AiRcaService.Infrastructure.Loki;
using HospitalSystem.AiRcaService.Infrastructure.Redaction;
using HospitalSystem.AiRcaService.Infrastructure.Rendering;
using HospitalSystem.AiRcaService.Infrastructure.Session;

namespace HospitalSystem.AiRcaService.Application.Services;

/// <summary>
/// Orchestrates the full pipeline:
/// cache-check → parallel(Loki + Jaeger) → PII redact → LLM tool_use → render HTML → cache-set → session-create.
/// </summary>
public sealed class LogAnalysisService : ILogAnalysisService
{
    private static readonly TimeSpan CacheTtl   = TimeSpan.FromHours(1);
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);

    private readonly ILokiClient _lokiClient;
    private readonly IPiiRedactor _piiRedactor;
    private readonly ILlmProvider _llmProvider;
    private readonly IHtmlRenderer _htmlRenderer;
    private readonly IAnalysisCache _cache;
    private readonly IJaegerClient _jaegerClient;
    private readonly IConversationStore _conversationStore;
    private readonly IConfiguration _config;
    private readonly ILogger<LogAnalysisService> _logger;

    public LogAnalysisService(
        ILokiClient lokiClient,
        IPiiRedactor piiRedactor,
        ILlmProvider llmProvider,
        IHtmlRenderer htmlRenderer,
        IAnalysisCache cache,
        IJaegerClient jaegerClient,
        IConversationStore conversationStore,
        IConfiguration config,
        ILogger<LogAnalysisService> logger)
    {
        _lokiClient        = lokiClient;
        _piiRedactor       = piiRedactor;
        _llmProvider       = llmProvider;
        _htmlRenderer      = htmlRenderer;
        _cache             = cache;
        _jaegerClient      = jaegerClient;
        _conversationStore = conversationStore;
        _config            = config;
        _logger            = logger;
    }

    public async Task<AnalysisResult> AnalyzeAsync(
        string service,
        DateTime from,
        DateTime to,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(service, from, to);

        // Step 1: cache lookup (HTML cache keyed on params)
        var cacheKey = RedisAnalysisCache.BuildKey(service, from, to, traceId);
        var cached   = await _cache.GetHtmlAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT for key={Key} — creating fresh session for cached result", cacheKey);
            // Still create a new conversation session so follow-up works even on cache hit
            var sessionId = await CreateSessionAsync(service, cached, cancellationToken);
            var htmlWithSession = _htmlRenderer.InjectSessionId(cached, sessionId);
            return new AnalysisResult(htmlWithSession, sessionId);
        }

        _logger.LogInformation(
            "Cache MISS. Starting RCA. Service={Service} From={From} To={To} TraceId={TraceId}",
            service, from, to, traceId ?? "none");

        var maxLines = _config.GetValue<int>("Analysis:MaxLogLines", 5000);

        // Step 2: parallel fetch Loki + Jaeger
        var lokiTask   = _lokiClient.FetchLogsAsync(service, from, to, maxLines, cancellationToken);
        var jaegerTask = traceId is not null
            ? _jaegerClient.FetchTraceSummaryAsync(traceId, cancellationToken)
            : Task.FromResult<string?>(null);

        await Task.WhenAll(lokiTask, jaegerTask);

        var rawLogs      = lokiTask.Result;
        var traceSummary = jaegerTask.Result;

        _logger.LogInformation(
            "Fetched {Count} log entries from Loki. Jaeger trace={HasTrace}",
            rawLogs.Count, traceSummary is not null);

        if (rawLogs.Count == 0)
        {
            _logger.LogWarning("No logs found for service={Service} in specified time range", service);
            var noLogHtml = _htmlRenderer.Render(
                "## No Logs Found\n\nNo log entries were returned from Loki for the specified service and time range. " +
                "Verify the service name and time range, then try again.",
                service, from, to, 0);
            // No session for empty-log responses — nothing to follow up on
            return new AnalysisResult(noLogHtml, string.Empty);
        }

        // Step 3: PII redact
        var redactResult = _piiRedactor.Redact(rawLogs);
        _logger.LogInformation("PII redaction complete. Redacted={Count}", redactResult.TotalRedactions);

        // Step 4: Build prompt and call LLM with tool_use
        var userPrompt = PromptTemplates.BuildUserPrompt(service, from, to, redactResult.RedactedEntries, traceSummary);
        var analysis   = await _llmProvider.AnalyzeStructuredAsync(
            PromptTemplates.SystemPrompt, userPrompt, cancellationToken);

        _logger.LogInformation("LLM structured analysis complete. Confidence={Confidence}", analysis.Confidence);

        // Step 5: Render HTML from structured output (without sessionId — injected after session creation)
        var metadata      = new RenderMetadata(service, from, to, redactResult.TotalRedactions);
        var htmlNoSession = _htmlRenderer.RenderStructured(analysis, metadata, sessionId: null);

        // Step 6: Cache the sessionless HTML (so cached responses get fresh sessions on hit)
        await _cache.SetHtmlAsync(cacheKey, htmlNoSession, CacheTtl, cancellationToken);

        // Step 7: Create conversation session — seed with structured summary as assistant context
        var sessionIdNew     = await CreateSessionFromAnalysisAsync(service, analysis, userPrompt, cancellationToken);
        var htmlWithSessionId = _htmlRenderer.InjectSessionId(htmlNoSession, sessionIdNew);

        return new AnalysisResult(htmlWithSessionId, sessionIdNew);
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    /// <summary>Creates a conversation seeded with the analysis context (assistant turn).</summary>
    private async Task<string> CreateSessionFromAnalysisAsync(
        string service,
        Infrastructure.Llm.StructuredAnalysis analysis,
        string userPrompt,
        CancellationToken ct)
    {
        var conv = new Conversation();

        // Seed: store the original user prompt as the first user message
        conv.AddMessage("user", userPrompt);

        // Seed: store a compact summary as the first assistant message
        var summary = $"Root cause: {analysis.RootCause}\n" +
                      $"Confidence: {analysis.Confidence}\n" +
                      $"Suggested fix: {analysis.SuggestedFix}";
        conv.AddMessage("assistant", summary);

        await _conversationStore.SaveAsync(conv, SessionTtl, ct);
        _logger.LogDebug("Created session={SessionId} for service={Service}", conv.SessionId, service);
        return conv.SessionId;
    }

    /// <summary>Creates a minimal conversation for cache-hit responses.</summary>
    private async Task<string> CreateSessionAsync(string service, string cachedHtml, CancellationToken ct)
    {
        var conv = new Conversation();
        conv.AddMessage("assistant", $"[Cached analysis result for service: {service}]");
        await _conversationStore.SaveAsync(conv, SessionTtl, ct);
        return conv.SessionId;
    }

    private void ValidateRequest(string service, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(service))
            throw new ArgumentException("Service name is required.", nameof(service));

        if (from >= to)
            throw new ArgumentException("'from' must be earlier than 'to'.");

        var maxMinutes = _config.GetValue<int>("Analysis:MaxWindowMinutes", 60);
        if ((to - from).TotalMinutes > maxMinutes)
            throw new ArgumentException(
                $"Time window exceeds maximum of {maxMinutes} minutes.");
    }
}
