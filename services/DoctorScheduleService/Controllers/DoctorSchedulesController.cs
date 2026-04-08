using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Application.Queries;
using HospitalShared.Auth;
using HospitalShared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoctorScheduleService.Controllers;

[ApiController]
[Route("api/doctor-schedules")]
[Authorize(Roles = Roles.AdminDoctor)]
public class DoctorSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DoctorSchedulesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new doctor schedule with auto-generated time slots.</summary>
    [HttpPost]
    public async Task<ActionResult<DoctorScheduleDto>> Create(
        [FromBody] CreateScheduleCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get a schedule by ID, including its slots.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DoctorScheduleDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetScheduleQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>List schedules with optional filters for doctorId and date.</summary>
    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] string? doctorId,
        [FromQuery] DateTime? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListSchedulesQuery(doctorId, date, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    /// <summary>Get all available slots for a schedule.</summary>
    [HttpGet("{id:guid}/slots/available")]
    public async Task<ActionResult<List<TimeSlotDto>>> GetAvailableSlots(Guid id, CancellationToken ct)
    {
        var slots = await _mediator.Send(new GetAvailableSlotsQuery(id), ct);
        return Ok(slots);
    }

    /// <summary>Reserve a slot for a patient (holds slot for 15 minutes).</summary>
    [HttpPost("{scheduleId:guid}/slots/{slotId:guid}/reserve")]
    public async Task<ActionResult<TimeSlotDto>> ReserveSlot(
        Guid scheduleId, Guid slotId,
        [FromBody] ReserveSlotRequest body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ReserveSlotCommand(scheduleId, slotId, body.PatientId), ct);
        return Ok(result);
    }

    /// <summary>Confirm a reserved slot by linking it to an appointment.</summary>
    [HttpPost("{scheduleId:guid}/slots/{slotId:guid}/confirm")]
    public async Task<ActionResult<TimeSlotDto>> ConfirmSlot(
        Guid scheduleId, Guid slotId,
        [FromBody] ConfirmSlotRequest body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ConfirmSlotCommand(scheduleId, slotId, body.AppointmentId), ct);
        return Ok(result);
    }

    /// <summary>Release a slot back to Available (cancels reservation).</summary>
    [HttpDelete("{scheduleId:guid}/slots/{slotId:guid}/reservation")]
    public async Task<IActionResult> ReleaseSlot(
        Guid scheduleId, Guid slotId, CancellationToken ct)
    {
        await _mediator.Send(new ReleaseSlotCommand(scheduleId, slotId), ct);
        return Ok(new { success = true, message = $"Slot {slotId} released successfully." });
    }
}

/// <summary>Request body for slot reservation.</summary>
public record ReserveSlotRequest(Guid PatientId);

/// <summary>Request body for slot confirmation.</summary>
public record ConfirmSlotRequest(Guid AppointmentId);
