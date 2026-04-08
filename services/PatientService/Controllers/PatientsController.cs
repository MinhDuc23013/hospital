using HospitalShared.Auth;
using HospitalShared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientService.Application.Commands;
using PatientService.Application.Queries;

namespace PatientService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AdminDoctorReceptionist)]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PatientsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create([FromBody] CreatePatientCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPatientQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(new ListPatientsQuery(page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, [FromBody] UpdatePatientCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeletePatientCommand(id), ct);
        return Ok(new { success = true, message = "Patient deleted successfully." });
    }
}
