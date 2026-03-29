using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;

namespace SearchServiceDotnet.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ElasticsearchClient _esClient;

    public HealthController(ElasticsearchClient esClient)
    {
        _esClient = esClient;
    }

    /// <summary>
    /// GET /health — returns service and Elasticsearch connectivity status.
    /// Returns 200 when healthy, 503 when Elasticsearch is unreachable.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        try
        {
            var ping = await _esClient.PingAsync(cancellationToken: ct);
            if (ping.IsValidResponse)
                return Ok(new { status = "healthy", elasticsearch = "connected" });

            return StatusCode(503, new { status = "degraded", elasticsearch = "unreachable" });
        }
        catch (Exception)
        {
            return StatusCode(503, new { status = "degraded", elasticsearch = "unreachable" });
        }
    }
}
