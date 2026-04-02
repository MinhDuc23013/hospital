using MediatR;
using Microsoft.AspNetCore.Mvc;
using MedicalRecordServiceDotnet.Application;
using MedicalRecordServiceDotnet.Application.Commands;
using MedicalRecordServiceDotnet.Application.Queries;

namespace MedicalRecordServiceDotnet.Controllers;

[ApiController]
[Route("api/medical-records")]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMediator _mediator;
    public MedicalRecordsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<MedicalRecordResult>> Create(
        [FromBody] CreateMedicalRecordCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalRecordResult>> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMedicalRecordByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult> GetByPatientId(
        string patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMedicalRecordsByPatientIdQuery(patientId, page, pageSize), ct);
        return Ok(new { data = result.Items, pagination = new { result.Total, result.Page, result.PageSize } });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MedicalRecordResult>> Update(
        string id, [FromBody] UpdateMedicalRecordCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await _mediator.Send(command, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
