using HospitalGateway.Extensions;
using HospitalShared.Metrics;
using HospitalShared.Tracing;
using HospitalGateway.Middleware;
using HospitalGateway.Services;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

// Structured logging with Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithSpan()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .WriteTo.GrafanaLoki(ctx.Configuration["Loki:Url"] ?? "http://localhost:3100",
        labels: [new() { Key = "service", Value = "hospital-gateway" }]));

// YARP reverse proxy — routes loaded from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT authentication via Keycloak
builder.Services.AddGatewayAuthentication(builder.Configuration);

// Rate limiting
builder.Services.AddGatewayRateLimiting();

// Health checks
builder.Services.AddHealthChecks();

// CORS for frontend origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:3100", "http://localhost:3200", "http://localhost:3400")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// MVC controllers — used for gateway-owned aggregate endpoints (e.g. booking-details)
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Hospital Gateway API", Version = "v1" });
});

// Custom Prometheus metrics (external_call_duration_seconds)
builder.Services.AddMetricsHttpHandler();
builder.Services.AddJaegerTracing(builder.Configuration, "hospital-gateway");

// Named HTTP client for downstream service calls — 15 s timeout suits fan-out aggregation
builder.Services.AddHttpClient("aggregation", c => c.Timeout = TimeSpan.FromSeconds(15))
    .AddMetricsHandler();

// Booking aggregation service
builder.Services.AddScoped<BookingAggregationService>();

// Redis for login brute-force protection
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital Gateway v1"));
}

// Middleware pipeline order matters
app.UseHttpMetrics();
app.UseSerilogRequestLogging();
app.UseCors();                           // before auth — let preflight OPTIONS pass
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Health check endpoint — no auth required (used by Docker + Prometheus)
app.MapHealthChecks("/health").AllowAnonymous();

// Gateway-owned controllers must be mapped before YARP so their routes take precedence
app.MapControllers();

// Prometheus metrics endpoint
app.MapMetrics().AllowAnonymous();

// All remaining traffic goes through YARP
app.MapReverseProxy();

app.Run();
