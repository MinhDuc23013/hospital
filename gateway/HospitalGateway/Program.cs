using HospitalGateway.Extensions;
using HospitalGateway.Middleware;
using HospitalGateway.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging with Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341"));

// YARP reverse proxy — routes loaded from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT authentication via Keycloak
builder.Services.AddGatewayAuthentication(builder.Configuration);

// Rate limiting
builder.Services.AddGatewayRateLimiting();

// Health checks
builder.Services.AddHealthChecks();

// MVC controllers — used for gateway-owned aggregate endpoints (e.g. booking-details)
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Hospital Gateway API", Version = "v1" });
});

// Named HTTP client for downstream service calls — 15 s timeout suits fan-out aggregation
builder.Services.AddHttpClient("aggregation", c => c.Timeout = TimeSpan.FromSeconds(15));

// Booking aggregation service
builder.Services.AddScoped<BookingAggregationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital Gateway v1"));
}

// Middleware pipeline order matters
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Health check endpoint — no auth required (used by Docker + Prometheus)
app.MapHealthChecks("/health").AllowAnonymous();

// Gateway-owned controllers must be mapped before YARP so their routes take precedence
app.MapControllers();

// All remaining traffic goes through YARP
app.MapReverseProxy();

app.Run();
