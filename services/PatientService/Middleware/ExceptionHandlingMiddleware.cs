using System.Text.Json;
using PatientService.Domain.Exceptions;

namespace PatientService.Middleware;

/// <summary>Catches exceptions and returns standardized JSON error responses.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var message = context.Request.Host.Host == "localhost"
                ? ex.Message : "An internal error occurred";
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
