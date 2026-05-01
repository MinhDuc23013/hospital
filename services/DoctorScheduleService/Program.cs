using Confluent.Kafka;
using DoctorScheduleService.Infrastructure.HttpClients;
using DoctorScheduleService.Infrastructure.MessageBus;
using DoctorScheduleService.Infrastructure.Persistence;
using DoctorScheduleService.Infrastructure.Repositories;
using DoctorScheduleService.Middleware;
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
        labels: [new() { Key = "service", Value = "doctor-schedule-service" }]));

// EF Core + PostgreSQL
builder.Services.AddDbContext<DoctorScheduleDbContext>(options =>
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
builder.Services.AddJaegerTracing(builder.Configuration, "doctor-schedule-service");

// Auth Service HTTP client
builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:AuthService"] ?? "http://auth-service:5009");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

// Elasticsearch client (direct search, no hop via SearchService)
var esUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
builder.Services.AddSingleton(new Elastic.Clients.Elasticsearch.ElasticsearchClient(
    new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(esUri))));

// Repositories & services
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<DoctorScheduleService.Infrastructure.Persistence.DoctorScheduleDbContext>>();

builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Doctor Schedule Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Doctor Schedule Service v1"));
}

app.MapControllers();
app.MapMetrics();
app.Run();
