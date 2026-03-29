using Microsoft.AspNetCore.Mvc;

namespace PatientService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "PatientService", status = "healthy", timestamp = DateTime.Now });
}
