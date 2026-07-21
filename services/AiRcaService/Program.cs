using HospitalShared.Tracing;
using HospitalSystem.AiRcaService.Application.Services;
using HospitalSystem.AiRcaService.Infrastructure.Cache;
using HospitalSystem.AiRcaService.Infrastructure.Jaeger;
using HospitalSystem.AiRcaService.Infrastructure.Llm;
using HospitalSystem.AiRcaService.Infrastructure.Loki;
using HospitalSystem.AiRcaService.Infrastructure.RateLimit;
using HospitalSystem.AiRcaService.Infrastructure.Redaction;
using HospitalSystem.AiRcaService.Infrastructure.Rendering;
using HospitalSystem.AiRcaService.Infrastructure.Session;
using HospitalSystem.AiRcaService.Middleware;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog (Console + Loki sink) ────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        ctx.Configuration["Loki:BaseUrl"] ?? "http://localhost:3100",
        labels: [new LokiLabel { Key = "service", Value = "ai-rca-service" }]));

// ── Distributed tracing (Jaeger via OpenTelemetry) ───────────────────────────
builder.Services.AddJaegerTracing(builder.Configuration, "ai-rca-service");

// ── Infrastructure ────────────────────────────────────────────────────────────
// Loki HTTP client with 30s timeout
builder.Services.AddHttpClient<ILokiClient, LokiClient>(client =>
{
    var baseUrl = builder.Configuration["Loki:BaseUrl"] ?? "http://loki:3100";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("Loki:TimeoutSeconds", 30));
});

// Internal LLM HTTP client — base address set at request time from config
builder.Services.AddHttpClient<ILlmProvider, InternalLlmProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddSingleton<IPiiRedactor, PiiRedactor>();
builder.Services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
builder.Services.AddSingleton<IHtmlRenderer, MarkdownHtmlRenderer>();

// Jaeger HTTP client (non-critical, 10s timeout, no retry)
builder.Services.AddHttpClient<IJaegerClient, JaegerClient>();

// Redis distributed cache
builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379");
builder.Services.AddSingleton<IAnalysisCache, RedisAnalysisCache>();
builder.Services.AddScoped<IConversationStore, RedisConversationStore>();

// ── Application ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<ILogAnalysisService, LogAnalysisService>();

// ── ASP.NET ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AI RCA Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI RCA Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();

// Expose for integration testing
public partial class Program { }
