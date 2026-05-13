using HospitalSystem.AiRcaService.Application;
using HospitalSystem.AiRcaService.Application.Services;
using HospitalSystem.AiRcaService.Infrastructure.RateLimit;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.AiRcaService.Controllers;

/// <summary>
/// Exposes the AI-assisted root cause analysis endpoint.
/// GET /api/ai-rca/analyze?service=x&amp;from=iso8601&amp;to=iso8601&amp;traceId=optional
/// </summary>
[ApiController]
[Route("api/ai-rca")]
public sealed class AnalyzeController : ControllerBase
{
    private readonly ILogAnalysisService _analysisService;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<AnalyzeController> _logger;

    public AnalyzeController(
        ILogAnalysisService analysisService,
        IRateLimiter rateLimiter,
        ILogger<AnalyzeController> logger)
    {
        _analysisService = analysisService;
        _rateLimiter     = rateLimiter;
        _logger          = logger;
    }

    /// <summary>Analyze logs for a service and return an HTML root-cause report.</summary>
    [HttpGet("analyze")]
    [Produces("text/html")]
    public async Task<IActionResult> AnalyzeAsync(
        [FromQuery] AnalyzeRequestDto req,
        CancellationToken cancellationToken)
    {
        // Validate required params
        if (string.IsNullOrWhiteSpace(req.Service))
            return BadRequest(new { error = "Query parameter 'service' is required." });

        if (req.From == default || req.To == default)
            return BadRequest(new { error = "Query parameters 'from' and 'to' are required (ISO 8601)." });

        // Rate limit per client IP
        var remoteIp     = HttpContext.Connection.RemoteIpAddress;
        var rateLimitKey = remoteIp?.ToString() ?? "unknown";
        // Mask IP for audit log: last IPv4 octet zeroed; IPv6 logged as masked
        var clientIp = remoteIp is { IsIPv4MappedToIPv6: false } && remoteIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? string.Join(".", remoteIp.ToString().Split('.')[..^1]) + ".0"
            : "masked";
        if (!_rateLimiter.TryAcquire(rateLimitKey))
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientIp}", clientIp);
            Response.Headers["Retry-After"] = "3600";
            return StatusCode(429, new { error = "Rate limit exceeded. Maximum 10 requests per hour." });
        }

        _logger.LogInformation(
            "Analyze request received. Service={Service} From={From} To={To} Client={Client}",
            req.Service, req.From, req.To, clientIp);

        var result = await _analysisService.AnalyzeAsync(
            req.Service, req.From, req.To, req.TraceId, cancellationToken);

        return Content(result.Html, "text/html");
    }
}
