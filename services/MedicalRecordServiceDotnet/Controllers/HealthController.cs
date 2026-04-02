using Microsoft.AspNetCore.Mvc;

namespace MedicalRecordServiceDotnet.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "medical-record-service" });
}
