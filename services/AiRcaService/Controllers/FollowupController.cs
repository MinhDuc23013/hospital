using System.Net;
using System.Text;
using HospitalSystem.AiRcaService.Domain;
using HospitalSystem.AiRcaService.Infrastructure.Llm;
using HospitalSystem.AiRcaService.Infrastructure.RateLimit;
using HospitalSystem.AiRcaService.Infrastructure.Session;
using Markdig;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.AiRcaService.Controllers;

/// <summary>
/// Handles follow-up Q&amp;A requests for an active RCA session.
/// POST /api/ai-rca/followup
/// Body (form): sessionId={id}&amp;question={text}
/// Returns an HTML fragment (two bubbles: user + assistant) for HTMX beforeend swap.
/// </summary>
[ApiController]
[Route("api/ai-rca")]
public sealed class FollowupController : ControllerBase
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);
    private const int MaxTurnsPerSession = 10;

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly IConversationStore _conversationStore;
    private readonly ILlmProvider _llmProvider;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<FollowupController> _logger;

    public FollowupController(
        IConversationStore conversationStore,
        ILlmProvider llmProvider,
        IRateLimiter rateLimiter,
        ILogger<FollowupController> logger)
    {
        _conversationStore = conversationStore;
        _llmProvider       = llmProvider;
        _rateLimiter       = rateLimiter;
        _logger            = logger;
    }

    /// <summary>
    /// Accepts a follow-up question, calls LLM with full conversation history,
    /// appends to session, and returns an HTML fragment for HTMX injection.
    /// </summary>
    [HttpPost("followup")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("text/html")]
    public async Task<IActionResult> FollowupAsync(
        [FromForm] string sessionId,
        [FromForm] string question,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Content(ErrorFragment("Session ID is required."), "text/html");

        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length < 3)
            return Content(ErrorFragment("Question must be at least 3 characters."), "text/html");

        // Rate limit — shared counter with /analyze per client IP
        var remoteIp     = HttpContext.Connection.RemoteIpAddress;
        var rateLimitKey = remoteIp?.ToString() ?? "unknown";
        if (!_rateLimiter.TryAcquire(rateLimitKey))
        {
            _logger.LogWarning("Rate limit exceeded for follow-up client {Ip}", rateLimitKey);
            Response.Headers["Retry-After"] = "3600";
            return Content(ErrorFragment("Rate limit exceeded. Maximum 10 requests per hour."), "text/html");
        }

        // Load session
        var conversation = await _conversationStore.GetAsync(sessionId, cancellationToken);
        if (conversation is null)
        {
            _logger.LogWarning("Session not found or expired. SessionId={SessionId}", sessionId);
            return Content(
                "<p class=\"error\">Session expired. Please start a new analysis.</p>",
                "text/html");
        }

        // Enforce max-turns cap
        var userTurns = conversation.Messages.Count(m => m.Role == "user");
        if (userTurns >= MaxTurnsPerSession)
        {
            return Content(
                ErrorFragment($"Maximum {MaxTurnsPerSession} follow-up questions reached for this session."),
                "text/html");
        }

        var trimmedQuestion = question.Trim();

        // Build multi-turn messages array from trimmed history
        var history = conversation.GetTrimmedHistory(MaxTurnsPerSession);
        var messages = history
            .Select(m => new LlmMessage(m.Role, m.Content))
            .Append(new LlmMessage("user", trimmedQuestion))
            .ToList();

        _logger.LogInformation(
            "Follow-up request. SessionId={SessionId} Turn={Turn} QuestionLength={Len}",
            sessionId, userTurns + 1, trimmedQuestion.Length);

        // Call LLM with full multi-turn context
        LlmResponse llmResponse;
        try
        {
            llmResponse = await _llmProvider.AnalyzeMultiTurnAsync(
                FollowupSystemPrompt, messages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM call failed for follow-up sessionId={SessionId}", sessionId);
            return Content(ErrorFragment("AI service temporarily unavailable. Please try again."), "text/html");
        }

        // Persist updated conversation
        conversation.AddMessage("user", trimmedQuestion);
        conversation.AddMessage("assistant", llmResponse.Markdown);
        await _conversationStore.SaveAsync(conversation, SessionTtl, cancellationToken);

        _logger.LogInformation(
            "Follow-up complete. SessionId={SessionId} InputTokens={In} OutputTokens={Out}",
            sessionId, llmResponse.InputTokens, llmResponse.OutputTokens);

        // Render HTML fragment — markdown → HTML for assistant reply
        var answerHtml = Markdown.ToHtml(llmResponse.Markdown, MarkdownPipeline);
        return Content(BuildFragment(trimmedQuestion, answerHtml), "text/html");
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    private static string BuildFragment(string question, string answerHtml)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"message user-message\">");
        sb.AppendLine($"  <strong>You:</strong> {WebUtility.HtmlEncode(question)}");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"message assistant-message\">");
        sb.AppendLine($"  <strong>AI:</strong> {answerHtml}");
        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string ErrorFragment(string message) =>
        $"<p class=\"error\">{WebUtility.HtmlEncode(message)}</p>";

    private const string FollowupSystemPrompt =
        "You are an expert SRE/DevOps assistant helping a developer investigate a system incident. " +
        "You have already provided an initial root cause analysis. " +
        "Answer follow-up questions concisely and technically. " +
        "Use markdown for code blocks and lists. " +
        "Do not repeat the full analysis unless specifically asked. " +
        "If you are unsure, say so clearly rather than guessing.";
}
