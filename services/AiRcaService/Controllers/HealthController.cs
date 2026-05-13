using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.AiRcaService.Controllers;

/// <summary>Health check endpoint — used by docker-compose and load balancers.</summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
        => Ok(new { status = "healthy", service = "ai-rca-service", timestamp = DateTime.UtcNow });
}
