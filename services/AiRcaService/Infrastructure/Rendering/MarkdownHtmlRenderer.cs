using System.Net;
using System.Text;
using HospitalSystem.AiRcaService.Infrastructure.Llm;
using Markdig;

namespace HospitalSystem.AiRcaService.Infrastructure.Rendering;

/// <summary>
/// Renders AI analysis into a self-contained HTML page with HTMX follow-up Q&amp;A.
/// Supports both raw markdown (legacy) and typed StructuredAnalysis output.
/// No external CSS/JS dependencies except HTMX CDN (acceptable for internal MVP tool).
/// </summary>
public sealed class MarkdownHtmlRenderer : IHtmlRenderer
{
    /// <summary>Placeholder replaced by InjectSessionId after session is created.</summary>
    internal const string SessionPlaceholder = "__SESSION_PLACEHOLDER__";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string Render(
        string markdown,
        string service,
        DateTime from,
        DateTime to,
        int redactionCount)
    {
        var bodyHtml   = Markdown.ToHtml(markdown, Pipeline);
        var redactNote = BuildRedactNote(redactionCount);
        // Legacy render: no session / no HTMX
        return BuildPage(service, from, to, redactNote, bodyHtml, sessionId: null);
    }

    public string RenderStructured(StructuredAnalysis analysis, RenderMetadata metadata, string? sessionId = null)
    {
        var redactNote = BuildRedactNote(metadata.RedactionCount);
        var bodyHtml   = BuildStructuredBody(analysis);
        return BuildPage(metadata.Service, metadata.From, metadata.To, redactNote, bodyHtml, sessionId);
    }

    public string InjectSessionId(string html, string sessionId)
        => html.Replace(SessionPlaceholder, WebUtility.HtmlEncode(sessionId));

    // ── Private builders ──────────────────────────────────────────────────────────

    private static string BuildStructuredBody(StructuredAnalysis a)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<h2>Root Cause</h2>");
        sb.AppendLine($"<p>{WebUtility.HtmlEncode(a.RootCause)}</p>");

        sb.AppendLine("<h2>Evidence</h2><ul>");
        foreach (var item in a.Evidence)
        {
            var ts      = WebUtility.HtmlEncode(item.Timestamp);
            var logLine = WebUtility.HtmlEncode(item.LogLine);
            var reason  = WebUtility.HtmlEncode(item.Reasoning);
            sb.AppendLine($"<li><code>[{ts}]</code> {logLine} &mdash; <em>{reason}</em></li>");
        }
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Suggested Fix</h2>");
        sb.AppendLine($"<pre>{WebUtility.HtmlEncode(a.SuggestedFix)}</pre>");

        var confidenceClass = a.Confidence.ToLowerInvariant() switch
        {
            "high"   => "confidence-high",
            "medium" => "confidence-medium",
            _        => "confidence-low"
        };
        sb.AppendLine($"<div class=\"confidence {confidenceClass}\">");
        sb.AppendLine($"<strong>Confidence: {WebUtility.HtmlEncode(a.Confidence)}</strong>");
        if (!string.IsNullOrWhiteSpace(a.ConfidenceReasoning))
            sb.AppendLine($" &mdash; {WebUtility.HtmlEncode(a.ConfidenceReasoning)}");
        sb.AppendLine("</div>");

        if (a.RelatedServices.Count > 0)
        {
            sb.AppendLine("<h2>Related Services</h2><p>");
            foreach (var svc in a.RelatedServices)
                sb.AppendLine($"<span class=\"related-chip\">{WebUtility.HtmlEncode(svc)}</span>");
            sb.AppendLine("</p>");
        }

        return sb.ToString();
    }

    private static string BuildRedactNote(int redactionCount) =>
        redactionCount > 0
            ? $"<span class=\"redact-badge\">{redactionCount} PHI item(s) redacted</span>"
            : "<span class=\"redact-ok\">No PHI redactions</span>";

    private static string BuildPage(
        string service, DateTime from, DateTime to,
        string redactNote, string bodyHtml, string? sessionId)
    {
        // Use placeholder when sessionId not yet known (cache-hit path injects later)
        var embeddedSession = sessionId is not null
            ? WebUtility.HtmlEncode(sessionId)
            : SessionPlaceholder;

        var qaSection = BuildQaSection(embeddedSession);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>AI RCA &mdash; {{WebUtility.HtmlEncode(service)}}</title>
              <script src="https://unpkg.com/htmx.org@1.9.12" crossorigin="anonymous"></script>
              <style>
                body { font-family: system-ui, sans-serif; max-width: 900px; margin: 2rem auto; padding: 0 1rem; color: #1a1a1a; background: #f8f9fa; }
                header { background: #0d6efd; color: white; padding: 1rem 1.5rem; border-radius: 8px; margin-bottom: 1.5rem; }
                header h1 { margin: 0 0 4px; font-size: 1.3rem; }
                header .meta { font-size: 0.85rem; opacity: 0.88; }
                .redact-badge { background: #fff3cd; color: #856404; padding: 2px 8px; border-radius: 4px; font-size: 0.8rem; }
                .redact-ok { background: #d1e7dd; color: #0f5132; padding: 2px 8px; border-radius: 4px; font-size: 0.8rem; }
                main { background: white; padding: 1.75rem; border-radius: 8px; box-shadow: 0 1px 4px rgba(0,0,0,.1); }
                h2 { color: #0d6efd; border-bottom: 2px solid #e9ecef; padding-bottom: 6px; font-size: 1rem; }
                pre, code { background: #f1f3f5; border-radius: 4px; }
                pre { padding: 14px; overflow-x: auto; }
                code { padding: 2px 5px; font-size: 0.9em; }
                ul { padding-left: 20px; }
                li { margin-bottom: 6px; }
                .confidence { padding: 10px 16px; border-radius: 6px; margin: 16px 0; font-size: 0.95rem; }
                .confidence-high   { background: #d1e7dd; color: #0f5132; border: 1px solid #a3cfbb; }
                .confidence-medium { background: #fff3cd; color: #664d03; border: 1px solid #ffe083; }
                .confidence-low    { background: #f8d7da; color: #842029; border: 1px solid #f1aeb5; }
                .related-chip { display: inline-block; background: #ebf4ff; color: #2b6cb0; border-radius: 4px; padding: 0.2em 0.6em; margin: 0.2em; font-size: 0.85rem; }
                #conversation { display: flex; flex-direction: column; gap: 0.75rem; margin-top: 1.5rem; }
                .message { border-radius: 8px; padding: 0.75rem 1rem; line-height: 1.55; }
                .user-message { background: #ebf4ff; align-self: flex-end; max-width: 80%; }
                .assistant-message { background: #f0fff4; border-left: 3px solid #48bb78; }
                .followup-form { margin-top: 1rem; display: flex; gap: 0.5rem; align-items: flex-end; }
                .followup-form textarea { flex: 1; padding: 0.5rem; border: 1px solid #dee2e6; border-radius: 6px; font-family: inherit; font-size: 0.95rem; resize: vertical; min-height: 64px; }
                .followup-form button { padding: 0.55rem 1.1rem; background: #0d6efd; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 0.9rem; white-space: nowrap; }
                .followup-form button:hover { background: #0b5ed7; }
                .htmx-request .followup-form button, .followup-form button:disabled { opacity: 0.6; cursor: not-allowed; }
                .error { color: #842029; background: #f8d7da; padding: 0.6rem 1rem; border-radius: 6px; }
                footer { margin-top: 1.5rem; padding: 0.75rem 1rem; background: #fff3cd; border: 1px solid #ffc107; border-radius: 8px; font-size: 0.85rem; color: #664d03; }
              </style>
            </head>
            <body>
              <header>
                <h1>AI Root Cause Analysis &mdash; {{WebUtility.HtmlEncode(service)}}</h1>
                <div class="meta">
                  {{from:yyyy-MM-dd HH:mm}} UTC &rarr; {{to:yyyy-MM-dd HH:mm}} UTC
                  &nbsp;|&nbsp; {{redactNote}}
                  &nbsp;|&nbsp; Generated: {{DateTime.UtcNow:yyyy-MM-dd HH:mm}} UTC
                </div>
              </header>
              <main>
                {{bodyHtml}}
                {{qaSection}}
              </main>
              <footer>
                &#9888; <strong>AI suggestion &mdash; verify before action.</strong>
                MVP: do not use with production PHI logs. Session expires after 30 min idle.
              </footer>
            </body>
            </html>
            """;
    }

    private static string BuildQaSection(string embeddedSession) => $$"""
        <hr style="margin: 1.5rem 0; border: none; border-top: 1px solid #e9ecef;">
        <h2>Follow-up Questions</h2>
        <div id="conversation"></div>
        <form class="followup-form"
              hx-post="/api/ai-rca/followup"
              hx-target="#conversation"
              hx-swap="beforeend"
              hx-on::after-request="if(event.detail.successful){this.querySelector('textarea').value=''}"
              hx-include="[name='sessionId']">
          <input type="hidden" name="sessionId" value="{{embeddedSession}}">
          <textarea name="question" placeholder="Ask a follow-up question about this incident..." required minlength="3"></textarea>
          <button type="submit">Ask AI</button>
        </form>
        """;
}
