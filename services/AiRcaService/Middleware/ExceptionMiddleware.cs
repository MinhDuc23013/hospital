using System.Text.Json;

namespace HospitalSystem.AiRcaService.Middleware;

/// <summary>
/// Global exception handler — returns structured JSON error, logs unhandled exceptions.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            var error = new { error = new { message = ex.Message, code = "VALIDATION_ERROR" } };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var error = new { error = new { message = "Internal server error", code = "INTERNAL_ERROR" } };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
