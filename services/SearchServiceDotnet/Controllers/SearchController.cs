using HospitalShared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchServiceDotnet.Application.Services;

namespace SearchServiceDotnet.Controllers;

[ApiController]
[Route("api/search")]
[Authorize(Roles = Roles.AdminDoctorReceptionist)]
public class SearchController : ControllerBase
{
    private readonly SearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(SearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger        = logger;
    }

    /// <summary>
    /// Multi-field search with pagination.
    /// GET /api/search?q={query}&amp;type={patient|drug}&amp;page=1&amp;pageSize=50
    /// Returns { data: [...], pagination: { total, page, pageSize } }
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q        = "",
        [FromQuery] string type     = "patient",
        [FromQuery] int    page     = 1,
        [FromQuery] int    pageSize = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = new { message = "Query parameter 'q' is required.", code = "BAD_REQUEST" } });

        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        _logger.LogInformation("Search request: q={Q} type={Type} page={Page} pageSize={PageSize}", q, type, page, pageSize);

        if (type.Equals("drug", StringComparison.OrdinalIgnoreCase))
        {
            var (items, total) = await _searchService.SearchDrugsAsync(q, page, pageSize, ct);
            return Ok(new { data = items, pagination = new { total, page, pageSize } });
        }
        else
        {
            // Default to patient search
            var (items, total) = await _searchService.SearchPatientsAsync(q, page, pageSize, ct);
            return Ok(new { data = items, pagination = new { total, page, pageSize } });
        }
    }
}
