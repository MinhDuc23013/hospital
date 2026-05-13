using HospitalSystem.AiRcaService.Infrastructure.Llm;

namespace HospitalSystem.AiRcaService.Infrastructure.Rendering;

/// <summary>Metadata for the HTML report header/footer.</summary>
public sealed record RenderMetadata(string Service, DateTime From, DateTime To, int RedactionCount);

/// <summary>Renders AI analysis into a full HTML page.</summary>
public interface IHtmlRenderer
{
    /// <summary>Converts raw markdown into a styled HTML document (legacy / fallback).</summary>
    string Render(
        string markdown,
        string service,
        DateTime from,
        DateTime to,
        int redactionCount);

    /// <summary>
    /// Renders a typed StructuredAnalysis into a styled HTML document.
    /// Pass a non-null sessionId to embed the HTMX follow-up Q&amp;A section.
    /// </summary>
    string RenderStructured(StructuredAnalysis analysis, RenderMetadata metadata, string? sessionId = null);

    /// <summary>
    /// Injects a session ID into an already-rendered HTML page (e.g. cache-hit path).
    /// Replaces the placeholder value="__SESSION_PLACEHOLDER__" with the real session ID.
    /// </summary>
    string InjectSessionId(string html, string sessionId);
}
