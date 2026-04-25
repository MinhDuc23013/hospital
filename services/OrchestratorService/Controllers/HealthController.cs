using Microsoft.AspNetCore.Mvc;

namespace OrchestratorService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "OrchestratorService", status = "healthy", timestamp = DateTime.Now });
}
