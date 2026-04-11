using Confluent.Kafka;
using FluentValidation;
using HospitalShared.Auth;
using HospitalShared.Metrics;
using HospitalShared.Tracing;
using Microsoft.EntityFrameworkCore;
using PatientService.Infrastructure.HttpClients;
using PatientService.Infrastructure.MessageBus;
using PatientService.Infrastructure.Persistence;
using PatientService.Infrastructure.Repositories;
using PatientService.Middleware;
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
        labels: [new() { Key = "service", Value = "patient-service" }]));

// EF Core + PostgreSQL
builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
    .AddMetricsInterceptor());

// MediatR — scans current assembly for handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Kafka producer (audit/trace events)
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig { BootstrapServers = kafkaBootstrap, MessageTimeoutMs = 15000 };
    Log.Information("Kafka config: bootstrap={Bootstrap}", kafkaBootstrap);
    return new ProducerBuilder<string, string>(config).Build();
});

// Custom Prometheus metrics (external_call_duration_seconds)
builder.Services.AddMetricsHttpHandler();
builder.Services.AddJaegerTracing(builder.Configuration, "patient-service");

// Auth Service HTTP client
builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:AuthService"] ?? "http://auth-service:5009");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddMetricsHandler();

// Repositories & services
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<PatientService.Infrastructure.Persistence.PatientDbContext>>();

builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddControllers();

// Swagger — Development only
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Patient Service API", Version = "v1" });
});

var app = builder.Build();

// Ensure DB schema exists on startup (no migrations present)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Patient Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();
