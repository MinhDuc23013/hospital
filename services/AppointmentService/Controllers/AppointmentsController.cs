using AppointmentService.Application.Commands;
using AppointmentService.Application.Queries;
using AppointmentService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBookingSagaLogRepository _logRepo;

    public AppointmentsController(IMediator mediator, IBookingSagaLogRepository logRepo)
    {
        _mediator = mediator;
        _logRepo = logRepo;
    }

    /// <summary>Full booking saga: create appointment → reserve slot → pay → confirm → notify.</summary>
    [HttpPost("book")]
    public async Task<ActionResult<BookAppointmentResult>> Book(
        [FromBody] BookAppointmentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (result.Status is "AwaitingPayment" or "PaymentCompleted")
            return CreatedAtAction(nameof(GetById), new { id = result.AppointmentId }, result);
        return UnprocessableEntity(result);
    }

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

    /// <summary>Get saga audit logs for a booking.</summary>
    [HttpGet("book/{sagaId:guid}/logs")]
    public async Task<ActionResult> GetBookingLogs(Guid sagaId, CancellationToken ct)
    {
        var logs = await _logRepo.GetBySagaIdAsync(sagaId, ct);
        return Ok(logs);
    }
}
