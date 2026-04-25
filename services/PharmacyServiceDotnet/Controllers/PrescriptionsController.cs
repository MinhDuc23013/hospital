using HospitalShared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Queries;
using PharmacyServiceDotnet.Domain.Exceptions;

namespace PharmacyServiceDotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AdminDoctorPharmacist)]
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
        var result = await _mediator.Send(new GetPrescriptionQuery(id), ct)
            ?? throw new NotFoundException("Prescription", id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<PrescriptionResult>>> ListByAppointment(
        [FromQuery] Guid appointmentId, CancellationToken ct)
    {
        var results = await _mediator.Send(new ListPrescriptionsByAppointmentQuery(appointmentId), ct);
        return Ok(results);
    }

    [HttpPost("{id:guid}/dispense")]
    public async Task<ActionResult<PrescriptionResult>> Dispense(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DispensePrescriptionCommand(id), ct);
        return Ok(result);
    }
}
