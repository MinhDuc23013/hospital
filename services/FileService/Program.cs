using FileService.Application.Configuration;
using FileService.Application.Services;
using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Repositories;
using FileService.Middleware;
using HospitalShared.Metrics;
using HospitalShared.Tracing;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Sinks.Http.BatchFormatters;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.WithSpan()
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
        .WriteTo.GrafanaLoki(ctx.Configuration["Loki:Url"] ?? "http://localhost:3100",
            labels: [new() { Key = "service", Value = "file-service" }]);

    var logstashUrl = ctx.Configuration["Logstash:Url"];
    if (!string.IsNullOrWhiteSpace(logstashUrl))
        cfg.WriteTo.Http(
            logstashUrl,
            queueLimitBytes: null,
            textFormatter: new CompactJsonFormatter(),
            batchFormatter: new ArrayBatchFormatter(),
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
});

// FileStorage config (size cap + content-type/extension allowlists)
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
var fileStorageOptions = builder.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
    ?? new FileStorageOptions();

// EF Core + PostgreSQL
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
    .AddMetricsInterceptor());

// MediatR — scans current assembly for handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Custom Prometheus metrics (external_call_duration_seconds) + tracing
builder.Services.AddMetricsHttpHandler();
builder.Services.AddJaegerTracing(builder.Configuration, "file-service");

// Repositories & services
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<FileValidationService>();

// NOTE: Keycloak auth intentionally not wired up for this pass — endpoints are open.
builder.Services.AddControllers();

// Raise multipart/body size limits from configured cap
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = fileStorageOptions.MaxSizeBytes;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = fileStorageOptions.MaxSizeBytes;
});

// Swagger — Development only
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "File Service API", Version = "v1" });
});

var app = builder.Build();

// Ensure DB schema exists on startup (no migrations present)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();
