using System.Net;
using System.Text;
using System.Text.Json;

namespace HospitalSystem.AiRcaService.Infrastructure.Jaeger;

/// <summary>
/// HTTP client for Jaeger query API.
/// Fetches trace by ID, filters error spans, builds a summary string for LLM context.
/// No retry — non-critical path; 10s timeout.
/// </summary>
public sealed class JaegerClient : IJaegerClient
{
    private readonly HttpClient _http;
    private readonly ILogger<JaegerClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JaegerClient(HttpClient http, IConfiguration config, ILogger<JaegerClient> logger)
    {
        _http   = http;
        _logger = logger;

        var baseUrl        = config["Jaeger:BaseUrl"] ?? "http://jaeger:16686";
        var timeoutSeconds = config.GetValue<int>("Jaeger:TimeoutSeconds", 10);
        _http.BaseAddress  = new Uri(baseUrl);
        _http.Timeout      = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<string?> FetchTraceSummaryAsync(string traceId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"/api/traces/{traceId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Jaeger trace {TraceId} not found", traceId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json  = await response.Content.ReadAsStringAsync(ct);
            var trace = JsonSerializer.Deserialize<JaegerTraceResponse>(json, JsonOpts);

            if (trace?.Data is not { Count: > 0 })
            {
                _logger.LogDebug("Jaeger returned empty data for trace {TraceId}", traceId);
                return null;
            }

            return BuildSummary(traceId, trace.Data[0]);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Jaeger request timed out for trace {TraceId}", traceId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Jaeger unavailable — skipping trace context for {TraceId}", traceId);
            return null;
        }
    }

    private static string BuildSummary(string traceId, JaegerTraceData data)
    {
        var spans        = data.Spans;
        var errorSpans   = spans.Where(IsErrorSpan).ToList();
        var serviceChain = BuildServiceChain(spans, data.Processes);

        var sb = new StringBuilder();
        sb.AppendLine($"TRACE SUMMARY (traceId: {traceId}):");
        sb.AppendLine($"- Total spans: {spans.Count}");
        sb.AppendLine($"- Services involved: {serviceChain}");
        sb.AppendLine($"- Error spans: {errorSpans.Count}");

        foreach (var span in errorSpans)
        {
            var svcName   = data.Processes.TryGetValue(span.ProcessID, out var proc) ? proc.ServiceName : span.ProcessID;
            var durationMs = span.Duration / 1000;
            var statusCode = GetTagValue(span.Tags, "http.status_code");
            var errorMsg   = GetTagValue(span.Tags, "error.message") ?? GetTagValue(span.Tags, "error");
            var detail     = BuildSpanDetail(statusCode, durationMs, errorMsg);
            sb.AppendLine($"  • {svcName}.{span.OperationName} ({detail})");
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsErrorSpan(JaegerSpan span)
    {
        foreach (var tag in span.Tags)
        {
            if (tag.Key == "error" && tag.Value is true or "true")
                return true;
            if (tag.Key == "http.status_code")
            {
                var val = tag.Value?.ToString();
                if (int.TryParse(val, out var code) && code >= 500)
                    return true;
            }
        }
        return false;
    }

    private static string BuildServiceChain(IReadOnlyList<JaegerSpan> spans, Dictionary<string, JaegerProcess> processes)
    {
        // Preserve insertion order of services as seen in spans
        var seen     = new HashSet<string>(StringComparer.Ordinal);
        var services = new List<string>();

        foreach (var span in spans)
        {
            if (!processes.TryGetValue(span.ProcessID, out var proc)) continue;
            if (seen.Add(proc.ServiceName))
                services.Add(proc.ServiceName);
        }

        return services.Count > 0 ? string.Join(" → ", services) : "unknown";
    }

    private static string? GetTagValue(IReadOnlyList<JaegerTag> tags, string key)
    {
        var tag = tags.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
        return tag?.Value?.ToString();
    }

    private static string BuildSpanDetail(string? statusCode, long durationMs, string? errorMsg)
    {
        var parts = new List<string>();
        if (statusCode != null) parts.Add($"http.status_code={statusCode}");
        parts.Add($"duration={durationMs}ms");
        if (!string.IsNullOrEmpty(errorMsg)) parts.Add($"error=\"{errorMsg}\"");
        return string.Join(", ", parts);
    }
}
