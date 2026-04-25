using HospitalShared.Auth;
using LabTestService.Application.Commands;
using LabTestService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTestService.Controllers;

[ApiController]
[Route("api/lab-orders")]
[Authorize(Roles = Roles.AdminDoctorReceptionistPatient)]
public class LabOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabOrdersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new lab order for a patient.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLabOrderCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get a lab order by ID (includes all test items).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLabOrderQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>List lab orders with optional filtering and pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? appointmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListLabOrdersQuery(patientId, appointmentId, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    /// <summary>Submit results for one or more test items in an order.</summary>
    [HttpPut("{id:guid}/results")]
    public async Task<IActionResult> SubmitResults(Guid id, [FromBody] SubmitLabResultsCommand command, CancellationToken ct)
    {
        // Override OrderId from route parameter
        var result = await _mediator.Send(command with { OrderId = id }, ct);
        return Ok(result);
    }

    /// <summary>Cancel a lab order.</summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelLabOrderCommand(id), ct);
        return Ok(new { success = true, message = "Lab order cancelled." });
    }
}
