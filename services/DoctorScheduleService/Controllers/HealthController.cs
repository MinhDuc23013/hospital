using Microsoft.AspNetCore.Mvc;

namespace DoctorScheduleService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "DoctorScheduleService", status = "healthy", timestamp = DateTime.UtcNow });
}
