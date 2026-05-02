using AppointmentService.Application.Commands;
using AppointmentService.Application.Queries;
using AppointmentService.Infrastructure.HttpClients;
using HospitalShared.Auth;
using HospitalShared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AdminDoctorReceptionistPatient)]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PatientServiceClient _patientClient;

    public AppointmentsController(IMediator mediator, PatientServiceClient patientClient)
    {
        _mediator = mediator;
        _patientClient = patientClient;
    }

    /// <summary>Validate patient existence via cache (L1→L2→HTTP). Safe for load testing.</summary>
    [HttpGet("patients/{patientId:guid}/exists")]
    public async Task<IActionResult> PatientExists(Guid patientId, CancellationToken ct)
    {
        var patient = await _patientClient.GetPatientAsync(patientId, ct);
        return patient is null ? NotFound(new { patientId, exists = false }) : Ok(new { patientId, exists = true });
    }

    /// <summary>Schedule a new appointment directly (no saga).</summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Schedule(
        [FromBody] ScheduleAppointmentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAppointmentQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] Guid? patientId, [FromQuery] string? providerId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListAppointmentsQuery(patientId, providerId, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    /// <summary>Cancel appointment with refund + slot release.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<CancelAppointmentResult>> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelAppointmentCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Mark appointment as completed after consultation (doctor action).</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CompleteAppointmentCommand(id), ct);
        return Ok(new { success = true, message = "Appointment completed." });
    }

    /// <summary>Mark appointment as no-show (doctor/admin action, no refund).</summary>
    [HttpPost("{id:guid}/no-show")]
    public async Task<IActionResult> NoShow(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new NoShowAppointmentCommand(id), ct);
        return Ok(new { message = "Appointment marked as no-show." });
    }

    /// <summary>Confirm payment for a booking (called by payment webhook/callback).</summary>
    [HttpPost("confirm-payment/{paymentId:guid}")]
    public async Task<IActionResult> ConfirmPayment(Guid paymentId, CancellationToken ct)
    {
        await _mediator.Send(new ConfirmPaymentCommand(paymentId), ct);
        return Ok(new { message = "Payment confirmed, booking completed." });
    }

    /// <summary>Confirm appointment after async saga completes (called by OrchestratorService).</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ConfirmAppointmentCommand(id), ct);
        return Ok(new { success = true });
    }
}
