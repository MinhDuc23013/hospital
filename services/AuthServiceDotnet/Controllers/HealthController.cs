using Microsoft.AspNetCore.Mvc;

namespace AuthServiceDotnet.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "auth-service" });
}
