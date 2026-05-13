using FluentAssertions;
using HospitalSystem.AiRcaService.Infrastructure.Loki;
using Xunit;

namespace AiRcaService.Tests.Loki;

public sealed class LokiQueryBuilderTests
{
    [Fact]
    public void BuildQuery_WithServiceName_ReturnsStreamSelector()
    {
        var query = LokiQueryBuilder.BuildQuery("patient-service");
        query.Should().Be("{service=\"patient-service\"}");
    }

    [Fact]
    public void BuildQuery_ErrorsOnly_AppendsFilter()
    {
        var query = LokiQueryBuilder.BuildQuery("patient-service", errorsOnly: true);
        query.Should().Be("{service=\"patient-service\"} |= \"error\"");
    }

    [Fact]
    public void ToUnixNanoseconds_KnownDateTime_ReturnsCorrectNs()
    {
        // 2024-01-01 00:00:00 UTC = 1704067200000 ms = 1704067200000000000 ns
        var dt  = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ns  = LokiQueryBuilder.ToUnixNanoseconds(dt);
        ns.Should().Be("1704067200000000000");
    }

    [Fact]
    public void BuildQueryParams_ContainsAllRequiredKeys()
    {
        var from   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to     = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc);
        var @params = LokiQueryBuilder.BuildQueryParams("{service=\"x\"}", from, to, 500);

        @params.Should().ContainKey("query");
        @params.Should().ContainKey("start");
        @params.Should().ContainKey("end");
        @params.Should().ContainKey("limit");
        @params.Should().ContainKey("direction");
        @params["direction"].Should().Be("backward");
        @params["limit"].Should().Be("500");
    }

    [Fact]
    public void BuildQueryParams_StartBeforeEnd()
    {
        var from   = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var to     = new DateTime(2024, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        var @params = LokiQueryBuilder.BuildQueryParams("{service=\"x\"}", from, to, 100);

        long.Parse(@params["start"]).Should().BeLessThan(long.Parse(@params["end"]));
    }
}
