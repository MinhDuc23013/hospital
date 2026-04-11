using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HospitalShared.Tracing;

/// <summary>
/// Shared OpenTelemetry + Jaeger tracing setup for all services.
/// Instruments: ASP.NET Core incoming requests, HttpClient outgoing calls, EF Core queries.
/// Exports traces via OTLP (gRPC) to Jaeger collector.
/// </summary>
public static class JaegerTracingExtensions
{
    /// <summary>Register OpenTelemetry tracing with Jaeger exporter.</summary>
    /// <param name="services">DI container</param>
    /// <param name="configuration">App configuration (reads Jaeger:Endpoint)</param>
    /// <param name="serviceName">Logical service name shown in Jaeger UI</param>
    public static IServiceCollection AddJaegerTracing(
        this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var jaegerEndpoint = configuration["Jaeger:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    // Skip health-check & metrics endpoints to reduce noise
                    opts.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health") &&
                        !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(opts =>
                    opts.SetDbStatementForText = true)
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(jaegerEndpoint);
                    opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                }));

        return services;
    }
}
