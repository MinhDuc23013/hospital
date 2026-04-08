using HospitalShared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchServiceDotnet.Application.Services;

namespace SearchServiceDotnet.Controllers;

/// <summary>
/// Triggers a full or partial reindex of Elasticsearch from upstream microservices.
/// POST /api/reindex?type={all|patient|appointment|payment}
/// </summary>
[ApiController]
[Route("api/reindex")]
[Authorize(Roles = Roles.AdminOnly)]
public class ReindexController : ControllerBase
{
    private readonly ReindexService _reindexService;
    private readonly ILogger<ReindexController> _logger;

    public ReindexController(ReindexService reindexService, ILogger<ReindexController> logger)
    {
        _reindexService = reindexService;
        _logger         = logger;
    }

    /// <summary>
    /// Reindexes data from microservices into Elasticsearch.
    /// Query param <c>type</c>: all | patient | appointment | payment (default: all)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Reindex(
        [FromQuery] string type = "all",
        CancellationToken ct = default)
    {
        _logger.LogInformation("Reindex request received: type={Type}", type);

        int patients     = 0;
        int appointments = 0;
        int payments     = 0;

        switch (type.ToLowerInvariant())
        {
            case "patient":
                patients = await _reindexService.ReindexPatientsAsync(ct);
                break;

            case "appointment":
                appointments = await _reindexService.ReindexAppointmentsAsync(ct);
                break;

            case "payment":
                payments = await _reindexService.ReindexPaymentsAsync(ct);
                break;

            case "all":
            default:
                var summary  = await _reindexService.ReindexAllAsync(ct);
                patients     = summary.Patients;
                appointments = summary.Appointments;
                payments     = summary.Payments;
                break;
        }

        return Ok(new
        {
            success = true,
            message = "Reindex completed",
            results = new
            {
                patients,
                appointments,
                payments
            }
        });
    }
}
