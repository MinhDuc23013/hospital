using Confluent.Kafka;
using FluentValidation;
using HospitalShared.Auth;
using HospitalShared.Metrics;
using HospitalShared.Resilience;
using HospitalShared.Tracing;
using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.HttpClients;
using PaymentService.Infrastructure.MessageBus;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Repositories;
using PaymentService.Middleware;
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
builder.Services.AddJaegerTracing(builder.Configuration, "payment-service");

// AppointmentService HTTP client for appointment validation
builder.Services.AddHttpClient<AppointmentServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:AppointmentService"] ?? "http://appointment-service:5002");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

// FakePaymentProvider HTTP client
builder.Services.AddHttpClient<IPaymentProviderClient, FakePaymentProviderClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PaymentProvider"] ?? "http://localhost:5009");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddResilienceHandler();

// Invoice aggregation HTTP clients
builder.Services.AddHttpClient<LabTestServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:LabTestService"] ?? "http://lab-test-service:5011");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

builder.Services.AddHttpClient<ImagingServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ImagingService"] ?? "http://imaging-service:5012");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

builder.Services.AddHttpClient<PharmacyServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PharmacyService"] ?? "http://pharmacy-service:5004");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddTokenForwarding().AddMetricsHandler().AddResilienceHandler();

// Repositories & services
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentAuditLogRepository, PaymentAuditLogRepository>();
builder.Services.AddScoped<ICashSessionRepository, CashSessionRepository>();
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

// Ensure cash_sessions table + receipt_seq sequence exist (dev — use migrations in prod)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS cash_sessions (
                ""Id"" uuid PRIMARY KEY,
                ""CashierId"" varchar(100) NOT NULL,
                ""CashierName"" varchar(200) NOT NULL,
                ""CounterId"" varchar(50) NOT NULL,
                ""OpeningBalance"" numeric(18,2) NOT NULL,
                ""ExpectedCash"" numeric(18,2) NOT NULL,
                ""ActualCash"" numeric(18,2) NULL,
                ""Variance"" numeric(18,2) NULL,
                ""Status"" varchar(20) NOT NULL,
                ""OpenedAt"" timestamp NOT NULL DEFAULT NOW(),
                ""ClosedAt"" timestamp NULL,
                ""Notes"" varchar(1000) NULL
            );
            CREATE INDEX IF NOT EXISTS ix_cash_sessions_cashier_status ON cash_sessions(""CashierId"", ""Status"");
            CREATE SEQUENCE IF NOT EXISTS receipt_seq START 1 INCREMENT 1;

            ALTER TABLE payments ADD COLUMN IF NOT EXISTS ""AmountReceived"" numeric(18,2) NULL;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS ""ChangeReturned"" numeric(18,2) NULL;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS ""CashierId"" varchar(100) NULL;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS ""CashSessionId"" uuid NULL;
            ALTER TABLE payments ADD COLUMN IF NOT EXISTS ""ReceiptNumber"" varchar(50) NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS ix_payments_receipt ON payments(""ReceiptNumber"") WHERE ""ReceiptNumber"" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_payments_cash_session ON payments(""CashSessionId"");
        ");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to ensure cash payment schema — may already exist");
    }
}

app.MapControllers();
app.MapMetrics();
app.Run();
