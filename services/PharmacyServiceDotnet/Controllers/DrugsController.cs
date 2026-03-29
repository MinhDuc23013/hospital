using MediatR;
using Microsoft.AspNetCore.Mvc;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Queries;

namespace PharmacyServiceDotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrugsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DrugsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<DrugResult>> Create([FromBody] CreateDrugCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DrugResult>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDrugQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] string? name, [FromQuery] bool? lowStock,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(new ListDrugsQuery(name, lowStock, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DrugResult>> Update(
        Guid id, [FromBody] UpdateDrugCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
