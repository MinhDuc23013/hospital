using MediatR;
using Microsoft.AspNetCore.Mvc;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Queries;

namespace PharmacyServiceDotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PrescriptionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<PrescriptionResult>> Create(
        [FromBody] CreatePrescriptionCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrescriptionResult>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPrescriptionQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/dispense")]
    public async Task<ActionResult<PrescriptionResult>> Dispense(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DispensePrescriptionCommand(id), ct);
        return Ok(result);
    }
}
