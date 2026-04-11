using System.Diagnostics;
using Prometheus;

namespace HospitalShared.Metrics;

/// <summary>
/// DelegatingHandler that records external_call_duration_seconds histogram
/// for every outgoing HTTP request made via HttpClient.
/// </summary>
public class MetricsHttpHandler : DelegatingHandler
{
    private static readonly Histogram ExternalCallDuration = Prometheus.Metrics.CreateHistogram(
        "external_call_duration_seconds",
        "Duration of outgoing HTTP calls to external services",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service", "method", "status_code" },
            Buckets = new[] { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
        });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var service = request.RequestUri?.Host ?? "unknown";
        var method = request.Method.Method;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            ExternalCallDuration
                .WithLabels(service, method, ((int)response.StatusCode).ToString())
                .Observe(sw.Elapsed.TotalSeconds);
            return response;
        }
        catch (Exception)
        {
            sw.Stop();
            ExternalCallDuration
                .WithLabels(service, method, "error")
                .Observe(sw.Elapsed.TotalSeconds);
            throw;
        }
    }
}
