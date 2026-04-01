using Confluent.Kafka;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Application.Saga;
using PharmacyServiceDotnet.Application.Services;
using PharmacyServiceDotnet.Infrastructure.HttpClients;
using PharmacyServiceDotnet.Infrastructure.MessageBus;
using PharmacyServiceDotnet.Infrastructure.Persistence;
using PharmacyServiceDotnet.Infrastructure.Repositories;
using PharmacyServiceDotnet.Infrastructure.Workers;
using PharmacyServiceDotnet.Middleware;
using Prometheus;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341"));

// EF Core + PostgreSQL
builder.Services.AddDbContext<PharmacyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

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

// Repositories
builder.Services.AddScoped<IDrugRepository, DrugRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IDrugBatchRepository, DrugBatchRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddScoped<IInventoryAuditLogRepository, InventoryAuditLogRepository>();
builder.Services.AddScoped<IDispensingSagaRepository, DispensingSagaRepository>();
builder.Services.AddScoped<IDispensingSagaLogRepository, DispensingSagaLogRepository>();

// Application services
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddScoped<StockReservationService>();
builder.Services.AddScoped<DispensingSagaOrchestrator>();

// PaymentService HTTP client
builder.Services.AddHttpClient<PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PaymentService"] ?? "http://payment-service:5008/");
});

// Background workers
builder.Services.AddHostedService<HospitalShared.Outbox.OutboxPublishWorker<PharmacyDbContext>>();
builder.Services.AddHostedService<ReservationExpiryWorker>();

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Pharmacy Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Prometheus metrics
app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmacy Service v1"));
}

app.MapControllers();
app.MapMetrics(); // /metrics endpoint for Prometheus
app.Run();
