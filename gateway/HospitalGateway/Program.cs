using HospitalGateway.Extensions;
using HospitalGateway.Middleware;
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

var app = builder.Build();

// Middleware pipeline order matters
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Health check endpoint — no auth required (used by Docker + Prometheus)
app.MapHealthChecks("/health").AllowAnonymous();

// All traffic goes through YARP
app.MapReverseProxy();

app.Run();
