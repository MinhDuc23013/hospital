using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationServiceDotnet.Application.Commands;

namespace NotificationServiceDotnet.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Send an email notification manually.</summary>
    [HttpPost("email")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailCommand command, CancellationToken ct)
    {
        var success = await _mediator.Send(command, ct);
        return success ? Ok(new { status = "sent" }) : StatusCode(500, new { status = "failed" });
    }

    /// <summary>Send an SMS notification manually.</summary>
    [HttpPost("sms")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsCommand command, CancellationToken ct)
    {
        var success = await _mediator.Send(command, ct);
        return success ? Ok(new { status = "sent" }) : StatusCode(500, new { status = "failed" });
    }
}
