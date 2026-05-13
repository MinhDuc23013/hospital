using FluentAssertions;
using HospitalSystem.AiRcaService.Infrastructure.Cache;
using Xunit;

namespace AiRcaService.Tests.Cache;

/// <summary>Verifies cache key determinism and uniqueness for RedisAnalysisCache.BuildKey.</summary>
public sealed class CacheKeyTests
{
    private static readonly DateTime From = new(2024, 6, 1, 8,  0, 0, DateTimeKind.Utc);
    private static readonly DateTime To   = new(2024, 6, 1, 8, 30, 0, DateTimeKind.Utc);

    // Case 1: Same inputs always produce the same key (determinism)
    [Fact]
    public void BuildKey_SameInputs_ProducesSameKey()
    {
        var key1 = RedisAnalysisCache.BuildKey("patient-service", From, To, "trace-abc");
        var key2 = RedisAnalysisCache.BuildKey("patient-service", From, To, "trace-abc");

        key1.Should().Be(key2);
    }

    // Case 2: Different service → different key
    [Fact]
    public void BuildKey_DifferentService_ProducesDifferentKey()
    {
        var key1 = RedisAnalysisCache.BuildKey("patient-service", From, To, null);
        var key2 = RedisAnalysisCache.BuildKey("billing-service",  From, To, null);

        key1.Should().NotBe(key2);
    }

    // Case 3: With vs without traceId → different key
    [Fact]
    public void BuildKey_WithAndWithoutTraceId_ProducesDifferentKeys()
    {
        var keyNoTrace   = RedisAnalysisCache.BuildKey("svc", From, To, null);
        var keyWithTrace = RedisAnalysisCache.BuildKey("svc", From, To, "trace-xyz");

        keyNoTrace.Should().NotBe(keyWithTrace);
    }

    // Case 4: Key has correct prefix and length
    [Fact]
    public void BuildKey_Format_HasAircaPrefixAndSixteenHexChars()
    {
        var key = RedisAnalysisCache.BuildKey("svc", From, To, null);

        key.Should().StartWith("airca:");
        // "airca:" (6) + 16 hex chars = 22 total
        key.Length.Should().Be(22);
        key["airca:".Length..].Should().MatchRegex("^[0-9a-f]{16}$");
    }

    // Case 5: Different time range → different key
    [Fact]
    public void BuildKey_DifferentTimeRange_ProducesDifferentKey()
    {
        var to2  = To.AddMinutes(5);
        var key1 = RedisAnalysisCache.BuildKey("svc", From, To,  null);
        var key2 = RedisAnalysisCache.BuildKey("svc", From, to2, null);

        key1.Should().NotBe(key2);
    }
}
