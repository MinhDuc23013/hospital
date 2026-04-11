using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalShared.Metrics;

/// <summary>
/// Extension methods to register custom Prometheus metrics
/// (db_query_duration_seconds, external_call_duration_seconds).
/// </summary>
public static class MetricsServiceExtensions
{
    /// <summary>Register MetricsHttpHandler so all HttpClient calls are instrumented.</summary>
    public static IServiceCollection AddMetricsHttpHandler(this IServiceCollection services)
    {
        services.AddTransient<MetricsHttpHandler>();
        return services;
    }

    /// <summary>Add MetricsHttpHandler to a specific HttpClient registration.</summary>
    public static IHttpClientBuilder AddMetricsHandler(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<MetricsHttpHandler>();
    }

    /// <summary>Add MetricsDbInterceptor to an EF Core DbContext.</summary>
    public static DbContextOptionsBuilder AddMetricsInterceptor(this DbContextOptionsBuilder options)
    {
        options.AddInterceptors(new MetricsDbInterceptor());
        return options;
    }
}
