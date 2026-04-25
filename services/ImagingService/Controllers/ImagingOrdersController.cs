using HospitalShared.Auth;
using ImagingService.Application.Commands;
using ImagingService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImagingService.Controllers;

[ApiController]
[Route("api/imaging-orders")]
[Authorize(Roles = Roles.AdminDoctorReceptionistPatient)]
public class ImagingOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ImagingOrdersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new imaging order for a patient.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateImagingOrderCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get a single imaging order by ID, including its result if available.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetImagingOrderQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>List imaging orders with optional filters and pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? appointmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListImagingOrdersQuery(patientId, appointmentId, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    /// <summary>Submit radiology result for a completed imaging order.</summary>
    [HttpPut("{id:guid}/result")]
    public async Task<IActionResult> SubmitResult(
        Guid id, [FromBody] SubmitImagingResultCommand command, CancellationToken ct)
    {
        var cmd = command with { OrderId = id };
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>Cancel a pending imaging order.</summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new CancelImagingOrderCommand(id), ct);
        return Ok(new { success });
    }
}
