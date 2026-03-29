using Microsoft.AspNetCore.Mvc;
using SearchServiceDotnet.Application.Services;

namespace SearchServiceDotnet.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(DashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger           = logger;
    }

    /// <summary>
    /// Returns aggregated dashboard metrics from Elasticsearch.
    /// GET /api/dashboard
    /// Response: { todayAppointments, monthRevenue, weekNewPatients, todayAvailableSlots }
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        _logger.LogInformation("Dashboard metrics requested");

        var metrics = await _dashboardService.GetDashboardAsync(ct);

        return Ok(new
        {
            todayAppointments   = metrics.TodayAppointments,
            monthRevenue        = metrics.MonthRevenue,
            weekNewPatients     = metrics.WeekNewPatients,
            todayAvailableSlots = metrics.TodayAvailableSlots
        });
    }
}
