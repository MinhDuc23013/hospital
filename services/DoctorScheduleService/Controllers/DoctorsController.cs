using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Application.Queries;
using HospitalShared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DoctorScheduleService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DoctorsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<DoctorDto>> Create(
        [FromBody] CreateDoctorCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DoctorDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDoctorQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] string? specialty, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListDoctorsQuery(specialty, isActive, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DoctorDto>> Update(
        Guid id, [FromBody] UpdateDoctorCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateDoctorCommand(id), ct);
        return Ok(new { success = true, message = "Doctor deactivated successfully." });
    }
}
