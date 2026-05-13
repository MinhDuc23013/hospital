using System.Text.Json;
using Polly;
using Polly.Extensions.Http;

namespace HospitalSystem.AiRcaService.Infrastructure.Loki;

/// <summary>
/// HTTP client for Grafana Loki query_range API.
/// Retries 3x with exponential backoff on transient errors.
/// </summary>
public sealed class LokiClient : ILokiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<LokiClient> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LokiClient(HttpClient http, ILogger<LokiClient> logger)
    {
        _http = http;
        _logger = logger;
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (outcome, delay, attempt, _) =>
                    logger.LogWarning("Loki retry {Attempt} after {Delay}s: {Error}",
                        attempt, delay.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()));
    }

    public async Task<IReadOnlyList<LokiLogEntry>> FetchLogsAsync(
        string service,
        DateTime from,
        DateTime to,
        int maxLines,
        CancellationToken cancellationToken = default)
    {
        var query  = LokiQueryBuilder.BuildQuery(service);
        var @params = LokiQueryBuilder.BuildQueryParams(query, from, to, maxLines);
        var qs     = string.Join("&", @params.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var url    = $"/loki/api/v1/query_range?{qs}";

        _logger.LogDebug("Querying Loki: {Url}", url);

        var response = await _retryPolicy.ExecuteAsync(
            ct => _http.GetAsync(url, ct), cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseLokiResponse(json);
    }

    private static IReadOnlyList<LokiLogEntry> ParseLokiResponse(string json)
    {
        using var doc    = JsonDocument.Parse(json);
        var results      = doc.RootElement.GetProperty("data").GetProperty("result");
        var entries      = new List<LokiLogEntry>();

        foreach (var stream in results.EnumerateArray())
        {
            // Extract stream labels
            var labels = new Dictionary<string, string>(StringComparer.Ordinal);
            if (stream.TryGetProperty("stream", out var streamLabels))
                foreach (var label in streamLabels.EnumerateObject())
                    labels[label.Name] = label.Value.GetString() ?? string.Empty;

            labels.TryGetValue("level", out var defaultLevel);

            // Extract values array: each element is [timestampNs, logLine]
            if (!stream.TryGetProperty("values", out var values)) continue;

            foreach (var val in values.EnumerateArray())
            {
                var arr = val.EnumerateArray().ToArray();
                if (arr.Length < 2) continue;

                var tsNs = long.Parse(arr[0].GetString() ?? "0");
                var msg  = arr[1].GetString() ?? string.Empty;
                var ts   = DateTimeOffset.FromUnixTimeMilliseconds(tsNs / 1_000_000).UtcDateTime;

                // Try to parse level from message prefix (e.g. "[ERR]", "level=error")
                var level = ExtractLevel(msg) ?? defaultLevel ?? "info";
                labels.TryGetValue("traceId", out var correlationId);

                entries.Add(new LokiLogEntry(ts, level, msg, new Dictionary<string, string>(labels), correlationId));
            }
        }

        return entries;
    }

    private static string? ExtractLevel(string message)
    {
        if (message.Contains("[ERR]", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("level=error", StringComparison.OrdinalIgnoreCase))
            return "error";
        if (message.Contains("[WRN]", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("level=warn", StringComparison.OrdinalIgnoreCase))
            return "warning";
        if (message.Contains("[INF]", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("level=info", StringComparison.OrdinalIgnoreCase))
            return "info";
        if (message.Contains("[DBG]", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("level=debug", StringComparison.OrdinalIgnoreCase))
            return "debug";
        if (message.Contains("[FTL]", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("level=fatal", StringComparison.OrdinalIgnoreCase))
            return "fatal";
        return null;
    }
}
