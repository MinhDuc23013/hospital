using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HospitalSystem.AiRcaService.Infrastructure.Jaeger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace AiRcaService.Tests.Jaeger;

/// <summary>Unit tests for JaegerClient: HTTP mocking + summary builder logic.</summary>
public sealed class JaegerClientTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jaeger:BaseUrl"]        = "http://jaeger:16686",
                ["Jaeger:TimeoutSeconds"] = "10"
            })
            .Build();

    private static JaegerClient BuildClient(HttpMessageHandler handler)
    {
        var http   = new HttpClient(handler);
        var config = BuildConfig();
        return new JaegerClient(http, config, NullLogger<JaegerClient>.Instance);
    }

    private static HttpMessageHandler MockHandler(HttpStatusCode status, string json)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        return mock.Object;
    }

    // Case 1: 404 → returns null
    [Fact]
    public async Task FetchTraceSummaryAsync_NotFound_ReturnsNull()
    {
        var client = BuildClient(MockHandler(HttpStatusCode.NotFound, "{}"));
        var result = await client.FetchTraceSummaryAsync("nonexistent-trace");
        result.Should().BeNull();
    }

    // Case 2: Empty data array → returns null
    [Fact]
    public async Task FetchTraceSummaryAsync_EmptyData_ReturnsNull()
    {
        var json   = JsonSerializer.Serialize(new { data = Array.Empty<object>() });
        var client = BuildClient(MockHandler(HttpStatusCode.OK, json));
        var result = await client.FetchTraceSummaryAsync("trace-abc");
        result.Should().BeNull();
    }

    // Case 3: Network/timeout exception → returns null (graceful degrade)
    [Fact]
    public async Task FetchTraceSummaryAsync_NetworkError_ReturnsNull()
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var client = BuildClient(mock.Object);
        var result = await client.FetchTraceSummaryAsync("trace-xyz");
        result.Should().BeNull();
    }

    // Case 4: Valid trace with error span → summary contains expected fields
    [Fact]
    public async Task FetchTraceSummaryAsync_ValidTrace_BuildsSummaryWithErrorSpans()
    {
        var traceJson = BuildSampleTraceJson(includeErrorSpan: true);
        var client    = BuildClient(MockHandler(HttpStatusCode.OK, traceJson));

        var result = await client.FetchTraceSummaryAsync("trace-001");

        result.Should().NotBeNull();
        result.Should().Contain("TRACE SUMMARY");
        result.Should().Contain("trace-001");
        result.Should().Contain("Total spans:");
        result.Should().Contain("Error spans: 1");
        result.Should().Contain("patient-service");
        result.Should().Contain("http.status_code=500");
    }

    // Case 5: Valid trace with NO error spans → error spans line shows 0
    [Fact]
    public async Task FetchTraceSummaryAsync_NoErrorSpans_ShowsZeroErrors()
    {
        var traceJson = BuildSampleTraceJson(includeErrorSpan: false);
        var client    = BuildClient(MockHandler(HttpStatusCode.OK, traceJson));

        var result = await client.FetchTraceSummaryAsync("trace-002");

        result.Should().NotBeNull();
        result.Should().Contain("Error spans: 0");
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string BuildSampleTraceJson(bool includeErrorSpan)
    {
        var spans = new List<object>
        {
            new
            {
                traceID       = "trace-001",
                spanID        = "span-1",
                operationName = "GET /api/patients",
                processID     = "p1",
                startTime     = 1_700_000_000_000L,
                duration      = 120_000L,
                tags          = Array.Empty<object>(),
                logs          = Array.Empty<object>()
            }
        };

        if (includeErrorSpan)
        {
            spans.Add(new
            {
                traceID       = "trace-001",
                spanID        = "span-2",
                operationName = "DB.Query",
                processID     = "p1",
                startTime     = 1_700_000_100_000L,
                duration      = 234_000L,
                tags          = new object[]
                {
                    new { key = "http.status_code", type = "int64", value = (object)500 },
                    new { key = "error.message",    type = "string", value = (object)"Connection timeout" }
                },
                logs = Array.Empty<object>()
            });
        }

        var payload = new
        {
            data = new[]
            {
                new
                {
                    traceID   = "trace-001",
                    spans     = spans,
                    processes = new Dictionary<string, object>
                    {
                        ["p1"] = new { serviceName = "patient-service", tags = Array.Empty<object>() }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }
}
