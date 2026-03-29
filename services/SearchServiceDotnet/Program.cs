using SearchServiceDotnet.Application.Services;
using SearchServiceDotnet.Infrastructure.Elasticsearch;
using SearchServiceDotnet.Infrastructure.Kafka;
using SearchServiceDotnet.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341"));

// ── Elasticsearch ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton(_ => ElasticsearchClientFactory.Create(builder.Configuration));

// ── Index initializer (creates indexes on startup, non-fatal) ─────────────────
builder.Services.AddHostedService<IndexInitializer>();

// ── Kafka consumers (BackgroundService) ───────────────────────────────────────
builder.Services.AddHostedService<PatientEventConsumer>();
builder.Services.AddHostedService<AppointmentEventConsumer>();
builder.Services.AddHostedService<PaymentEventConsumer>();
builder.Services.AddHostedService<SlotEventConsumer>();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddHttpClient<ReindexService>();
builder.Services.AddScoped<ReindexService>();

// ── MVC + Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Search Service API", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Search Service v1"));
}

app.MapControllers();
app.Run();
