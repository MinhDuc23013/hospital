using AppointmentService.Infrastructure.HttpClients;
using AppointmentService.Infrastructure.MessageBus;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Repositories;
using AppointmentService.Middleware;
using Confluent.Kafka;
using FluentValidation;
using HospitalShared.Auth;
using HospitalShared.Metrics;
using HospitalShared.Resilience;
using HospitalShared.Tracing;
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
        labels: [new() { Key = "service", Value = "appointment-service" }]));

// EF Core + PostgreSQL — write (primary)
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
    .AddMetricsInterceptor());

// EF Core + PostgreSQL — read replica (no tracking, no outbox)
builder.Services.AddDbContext<AppointmentReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQLRead"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
    .AddMetricsInterceptor());

// MediatR — scans current assembly for handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Kafka producer
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig { BootstrapServers = kafkaBootstrap, MessageTimeoutMs = 15000 };
    Log.Information("Kafka config: bootstrap={Bootstrap}", kafkaBootstrap);
    return new ProducerBuilder<string, string>(config).Build();
});

// Custom Prometheus metrics
builder.Services.AddMetricsHttpHandler();
builder.Services.AddJaegerTracing(builder.Configuration, "appointment-service");

// PatientService HTTP client for patient validation
builder.Services.AddHttpClient<PatientServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PatientService"] ?? "http://patient-service:5001");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

// DoctorScheduleService HTTP client
builder.Services.AddHttpClient<DoctorScheduleServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:DoctorScheduleService"] ?? "http://doctor-schedule-service:5007");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

// Repositories
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentReadRepository, AppointmentReadRepository>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddSingleton<HospitalShared.Kafka.KafkaDlqPublisher>();

// Background workers (outbox only — saga workers moved to OrchestratorService)
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<AppointmentDbContext>>();

builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddControllers();

// Swagger — Development only
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Appointment Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Appointment Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();
