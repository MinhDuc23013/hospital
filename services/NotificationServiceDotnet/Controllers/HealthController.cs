using Microsoft.AspNetCore.Mvc;

namespace NotificationServiceDotnet.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "notification-service" });
}
