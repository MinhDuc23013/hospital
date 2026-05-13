namespace HospitalSystem.AiRcaService.Infrastructure.Loki;

/// <summary>Queries Grafana Loki for log entries within a time range.</summary>
public interface ILokiClient
{
    /// <summary>
    /// Fetches log entries from Loki query_range API.
    /// Returns entries ordered newest-first (direction=backward).
    /// </summary>
    Task<IReadOnlyList<LokiLogEntry>> FetchLogsAsync(
        string service,
        DateTime from,
        DateTime to,
        int maxLines,
        CancellationToken cancellationToken = default);
}
