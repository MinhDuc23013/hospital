using Confluent.Kafka;
using FluentValidation;
using HospitalShared.Auth;
using HospitalShared.Metrics;
using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.HttpClients;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Repositories;
using PaymentService.Middleware;
using Prometheus;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .WriteTo.GrafanaLoki(ctx.Configuration["Loki:Url"] ?? "http://localhost:3100",
        labels: [new() { Key = "service", Value = "payment-service" }]));

// EF Core + PostgreSQL
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
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

// Custom Prometheus metrics (external_call_duration_seconds)
builder.Services.AddMetricsHttpHandler();

// AppointmentService HTTP client for appointment validation
builder.Services.AddHttpClient<AppointmentServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:AppointmentService"] ?? "http://appointment-service:5002");
    client.Timeout = TimeSpan.FromSeconds(5);
}).AddMetricsHandler();

// Repositories & services
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentAuditLogRepository, PaymentAuditLogRepository>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<PaymentService.Infrastructure.Persistence.PaymentDbContext>>();

builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Payment Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();
