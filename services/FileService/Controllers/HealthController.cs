using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { service = "FileService", status = "healthy", timestamp = DateTime.Now });
}
