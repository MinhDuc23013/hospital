using HospitalShared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;

namespace PharmacyServiceDotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AdminPharmacist)]
public class DispensingController : ControllerBase
{
    private readonly IMediator _mediator;
    public DispensingController(IMediator mediator) => _mediator = mediator;

    /// <summary>Start dispensing: create prescription → reserve stock (FEFO) → create payment → await payment.</summary>
    [HttpPost("start")]
    public async Task<ActionResult<DispensingSagaResult>> Start(
        [FromBody] StartDispensingCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Start), new { id = result.Id }, result);
    }

    /// <summary>Complete dispensing after payment confirmation — processes payment, commits stock, dispenses.</summary>
    [HttpPost("{sagaId:guid}/complete-payment")]
    public async Task<ActionResult<DispensingSagaResult>> CompletePayment(Guid sagaId, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteDispensingPaymentCommand(sagaId), ct);
        return Ok(result);
    }

    /// <summary>Cancel dispensing — releases reserved stock and cancels payment.</summary>
    [HttpPost("{sagaId:guid}/cancel")]
    public async Task<ActionResult<DispensingSagaResult>> Cancel(Guid sagaId, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelDispensingCommand(sagaId), ct);
        return Ok(result);
    }
}
