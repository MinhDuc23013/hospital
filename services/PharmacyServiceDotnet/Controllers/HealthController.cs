using Microsoft.AspNetCore.Mvc;

namespace PharmacyServiceDotnet.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "PharmacyService", status = "healthy", timestamp = DateTime.Now });
}
