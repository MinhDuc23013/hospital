using System.Text.Json;
using PatientService.Domain.Exceptions;

namespace PatientService.Middleware;

/// <summary>Catches exceptions and returns standardized JSON error responses.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteError(context, 404, ex.Message, "NOT_FOUND");
        }
        catch (DomainException ex)
        {
            await WriteError(context, 400, ex.Message, "DOMAIN_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var message = _env.IsDevelopment()
                ? $"{ex.Message}{(ex.InnerException != null ? $" --> {ex.InnerException.Message}" : "")}"
                : "An internal error occurred";
            await WriteError(context, 500, message, "INTERNAL_ERROR");
        }
    }

    private static Task WriteError(HttpContext ctx, int status, string message, string code)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new { error = new { message, code } });
        return ctx.Response.WriteAsync(body);
    }
}
