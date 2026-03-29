using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "AppointmentService", status = "healthy", timestamp = DateTime.Now });
}
