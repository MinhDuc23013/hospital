namespace HospitalSystem.AiRcaService.Infrastructure.Loki;

/// <summary>Builds LogQL query strings and URL query parameters for Loki query_range API.</summary>
public static class LokiQueryBuilder
{
    /// <summary>Builds a LogQL stream selector for a given service label.</summary>
    public static string BuildQuery(string service, bool errorsOnly = false)
    {
        var selector = $"{{service=\"{service}\"}}";
        return errorsOnly ? $"{selector} |= \"error\"" : selector;
    }

    /// <summary>Converts DateTime to Unix nanoseconds string required by Loki API.</summary>
    public static string ToUnixNanoseconds(DateTime dt)
    {
        var epochOffset = new DateTimeOffset(dt, TimeSpan.Zero);
        return (epochOffset.ToUnixTimeMilliseconds() * 1_000_000L).ToString();
    }

    /// <summary>Builds query string parameters for the query_range endpoint.</summary>
    public static Dictionary<string, string> BuildQueryParams(
        string logqlQuery,
        DateTime from,
        DateTime to,
        int limit)
    {
        return new Dictionary<string, string>
        {
            ["query"]     = logqlQuery,
            ["start"]     = ToUnixNanoseconds(from),
            ["end"]       = ToUnixNanoseconds(to),
            ["limit"]     = limit.ToString(),
            ["direction"] = "backward"
        };
    }
}
