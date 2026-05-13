namespace HospitalSystem.AiRcaService.Infrastructure.Cache;

/// <summary>Cache for rendered HTML analysis results to reduce LLM cost and latency.</summary>
public interface IAnalysisCache
{
    Task<string?> GetHtmlAsync(string key, CancellationToken ct = default);
    Task SetHtmlAsync(string key, string html, TimeSpan ttl, CancellationToken ct = default);
}
