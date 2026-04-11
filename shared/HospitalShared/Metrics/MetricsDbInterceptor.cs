using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;

namespace HospitalShared.Metrics;

/// <summary>
/// EF Core interceptor that records db_query_duration_seconds histogram
/// for every database command (queries, inserts, updates, deletes).
/// </summary>
public class MetricsDbInterceptor : DbCommandInterceptor
{
    private static readonly Histogram DbQueryDuration = Prometheus.Metrics.CreateHistogram(
        "db_query_duration_seconds",
        "Duration of database queries via EF Core",
        new HistogramConfiguration
        {
            LabelNames = new[] { "command_type", "status" },
            Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5 }
        });

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RecordDuration(eventData, "query");
        return new ValueTask<DbDataReader>(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        RecordDuration(eventData, "nonquery");
        return new ValueTask<int>(result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        RecordDuration(eventData, "scalar");
        return new ValueTask<object?>(result);
    }

    public override Task CommandFailedAsync(
        DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DbQueryDuration
            .WithLabels("error", "failed")
            .Observe(eventData.Duration.TotalSeconds);
        return Task.CompletedTask;
    }

    private static void RecordDuration(CommandExecutedEventData eventData, string commandType)
    {
        DbQueryDuration
            .WithLabels(commandType, "success")
            .Observe(eventData.Duration.TotalSeconds);
    }
}
