using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "PaymentService", status = "healthy", timestamp = DateTime.Now });
}
