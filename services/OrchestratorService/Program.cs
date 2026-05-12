using Confluent.Kafka;
using HospitalShared.Auth;
using HospitalShared.Caching;
using HospitalShared.Metrics;
using HospitalShared.Resilience;
using HospitalShared.Tracing;
using Microsoft.EntityFrameworkCore;
using Castle.DynamicProxy;
using OrchestratorService.Application.Saga;
using OrchestratorService.Infrastructure.BackgroundJobs;
using OrchestratorService.Infrastructure.Interceptors;
using OrchestratorService.Infrastructure.Consumers;
using OrchestratorService.Infrastructure.HttpClients;
using OrchestratorService.Infrastructure.MessageBus;
using OrchestratorService.Infrastructure.Persistence;
using OrchestratorService.Infrastructure.Repositories;
using OrchestratorService.Middleware;
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
    cfg .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.WithSpan()
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
        .WriteTo.GrafanaLoki(ctx.Configuration["Loki:Url"] ?? "http://localhost:3100",
            labels: [new() { Key = "service", Value = "orchestrator-service" }]);

    var logstashUrl = ctx.Configuration["Logstash:Url"];
    if (!string.IsNullOrWhiteSpace(logstashUrl))
        cfg.WriteTo.Http(
            logstashUrl,
            queueLimitBytes: null,
            textFormatter: new CompactJsonFormatter(),
            batchFormatter: new ArrayBatchFormatter(),
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
});

// EF Core + PostgreSQL — tables already exist, no EnsureCreated/Migrate calls
builder.Services.AddDbContext<OrchestratorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null))
    .AddMetricsInterceptor());

// MediatR — scans current assembly for handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

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
builder.Services.AddJaegerTracing(builder.Configuration, "orchestrator-service");
builder.Services.AddPatientFusionCache(builder.Configuration, instanceName: "orchestrator:");

// Timing interceptor (singleton — ProxyGenerator is thread-safe and expensive to create)
builder.Services.AddSingleton(new ProxyGenerator());
builder.Services.AddSingleton<TimingInterceptor>();

// HTTP clients — typed client registered for IHttpClientFactory; interface proxy wraps it for timing
builder.Services.AddHttpClient<PatientServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PatientService"] ?? "http://patient-service:5001");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();
builder.Services.AddScoped<IPatientServiceClient>(sp =>
    sp.GetRequiredService<ProxyGenerator>().CreateInterfaceProxyWithTarget<IPatientServiceClient>(
        sp.GetRequiredService<PatientServiceClient>(),
        sp.GetRequiredService<TimingInterceptor>()));

builder.Services.AddHttpClient<AppointmentServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:AppointmentService"] ?? "http://appointment-service:5002");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();
builder.Services.AddScoped<IAppointmentServiceClient>(sp =>
    sp.GetRequiredService<ProxyGenerator>().CreateInterfaceProxyWithTarget<IAppointmentServiceClient>(
        sp.GetRequiredService<AppointmentServiceClient>(),
        sp.GetRequiredService<TimingInterceptor>()));

builder.Services.AddHttpClient<DoctorScheduleServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:DoctorScheduleService"] ?? "http://doctor-schedule-service:5007");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();
builder.Services.AddScoped<IDoctorScheduleServiceClient>(sp =>
    sp.GetRequiredService<ProxyGenerator>().CreateInterfaceProxyWithTarget<IDoctorScheduleServiceClient>(
        sp.GetRequiredService<DoctorScheduleServiceClient>(),
        sp.GetRequiredService<TimingInterceptor>()));

builder.Services.AddHttpClient<PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PaymentService"] ?? "http://payment-service:5008");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();
builder.Services.AddScoped<IPaymentServiceClient>(sp =>
    sp.GetRequiredService<ProxyGenerator>().CreateInterfaceProxyWithTarget<IPaymentServiceClient>(
        sp.GetRequiredService<PaymentServiceClient>(),
        sp.GetRequiredService<TimingInterceptor>()));

// Repositories
builder.Services.AddScoped<IBookingSagaRepository, BookingSagaRepository>();
builder.Services.AddScoped<IBookingSagaLogRepository, BookingSagaLogRepository>();
builder.Services.AddScoped<ICompensationOutboxRepository, CompensationOutboxRepository>();
builder.Services.AddScoped<IPaymentSagaRepository, PaymentSagaRepository>();

// Message bus
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddSingleton<HospitalShared.Kafka.KafkaDlqPublisher>();

// Saga orchestrators — concrete registered for DI resolution; interface proxy wraps for timing
builder.Services.AddScoped<BookingSagaOrchestrator>();
builder.Services.AddScoped<IBookingSagaOrchestrator>(sp =>
    sp.GetRequiredService<ProxyGenerator>().CreateInterfaceProxyWithTarget<IBookingSagaOrchestrator>(
        sp.GetRequiredService<BookingSagaOrchestrator>(),
        sp.GetRequiredService<TimingInterceptor>()));

builder.Services.AddScoped<PaymentSagaOrchestrator>();
builder.Services.AddScoped<IPaymentSagaOrchestrator>(sp =>
    sp.GetRequiredService<ProxyGenerator>().CreateInterfaceProxyWithTarget<IPaymentSagaOrchestrator>(
        sp.GetRequiredService<PaymentSagaOrchestrator>(),
        sp.GetRequiredService<TimingInterceptor>()));

// Background workers
builder.Services.AddHostedService<CompensationRetryWorker>();
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<OrchestratorDbContext>>();
builder.Services.AddHostedService<BookingAsyncPhaseConsumer>();
builder.Services.AddHostedService<PaymentEventConsumer>();

builder.Services.AddKeycloakAuth(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Orchestrator Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orchestrator Service v1"));
}

// Ensure payment_sagas table exists (dev — use migrations in prod)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS payment_sagas (
                ""Id""            uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""PatientId""     uuid NOT NULL,
                ""Method""        varchar(50) NOT NULL,
                ""Currency""      varchar(10) NOT NULL DEFAULT 'VND',
                ""Amount""        numeric(18,2) NOT NULL DEFAULT 0,
                ""PaymentId""     uuid NULL,
                ""CheckoutUrl""   varchar(2000) NULL,
                ""CurrentStep""   varchar(20) NOT NULL,
                ""FailureReason"" varchar(1000) NULL,
                ""RetryCount""    int NOT NULL DEFAULT 0,
                ""CreatedAt""     timestamp NOT NULL DEFAULT NOW(),
                ""UpdatedAt""     timestamp NOT NULL DEFAULT NOW()
            );
            CREATE INDEX IF NOT EXISTS ix_payment_sagas_appointment ON payment_sagas(""AppointmentId"");
            CREATE INDEX IF NOT EXISTS ix_payment_sagas_payment ON payment_sagas(""PaymentId"");
            CREATE INDEX IF NOT EXISTS ix_payment_sagas_step ON payment_sagas(""CurrentStep"");
            CREATE UNIQUE INDEX IF NOT EXISTS uq_payment_sagas_active_appointment
                ON payment_sagas(""AppointmentId"")
                WHERE ""CurrentStep"" != 'Failed';
        ");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to ensure payment_sagas schema — may already exist");
    }
}

app.MapControllers();
app.MapMetrics();
app.Run();
