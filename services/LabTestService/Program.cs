using Confluent.Kafka;
using FluentValidation;
using HospitalShared.Auth;
using HospitalShared.Metrics;
using HospitalShared.Tracing;
using LabTestService.Infrastructure.MessageBus;
using LabTestService.Infrastructure.Persistence;
using LabTestService.Infrastructure.Repositories;
using LabTestService.Middleware;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .WriteTo.GrafanaLoki(ctx.Configuration["Loki:Url"] ?? "http://localhost:3100",
        labels: [new() { Key = "service", Value = "lab-test-service" }]));

// EF Core + PostgreSQL with retry and metrics interceptor
builder.Services.AddDbContext<LabTestDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
    .AddMetricsInterceptor());

// MediatR — scans current assembly for handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Kafka producer singleton
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig { BootstrapServers = kafkaBootstrap, MessageTimeoutMs = 15000 };
    Log.Information("Kafka config: bootstrap={Bootstrap}", kafkaBootstrap);
    return new ProducerBuilder<string, string>(config).Build();
});

// Custom Prometheus metrics + Jaeger tracing
builder.Services.AddMetricsHttpHandler();
builder.Services.AddJaegerTracing(builder.Configuration, "lab-test-service");

// Repositories and message bus
builder.Services.AddScoped<ILabOrderRepository, LabOrderRepository>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddSingleton<HospitalShared.Kafka.KafkaDlqPublisher>();

// Outbox background worker — drains outbox table and publishes to Kafka
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<LabTestDbContext>>();

builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Lab Test Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lab Test Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();
