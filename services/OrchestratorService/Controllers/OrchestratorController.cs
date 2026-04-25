using HospitalShared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestratorService.Application.Commands;
using OrchestratorService.Infrastructure.Repositories;

namespace OrchestratorService.Controllers;

[ApiController]
[Route("api/orchestrator")]
[Authorize(Roles = Roles.AdminDoctorReceptionistPatient)]
public class OrchestratorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBookingSagaLogRepository _logRepo;

    public OrchestratorController(IMediator mediator, IBookingSagaLogRepository logRepo)
    {
        _mediator = mediator;
        _logRepo = logRepo;
    }

    /// <summary>
    /// Hybrid booking saga.
    /// Sync phase:  validate patient → create appointment (HTTP) → lock slot → 202 SlotReserved.
    /// Async phase: runs in background via Kafka → confirm slot → confirm appointment → notify → index.
    /// </summary>
    [HttpPost("book")]
    public async Task<ActionResult<BookAppointmentResult>> Book(
        [FromBody] BookAppointmentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.Status switch
        {
            "SlotReserved" => Accepted(
                $"/api/orchestrator/book/{result.SagaId}/logs",
                result),
            "AwaitingPayment" or "PaymentCompleted" => CreatedAtAction(
                nameof(GetBookingLogs), new { sagaId = result.SagaId }, result),
            _ => UnprocessableEntity(result)
        };
    }

    /// <summary>Get saga audit logs for a booking.</summary>
    [HttpGet("book/{sagaId:guid}/logs")]
    public async Task<ActionResult> GetBookingLogs(Guid sagaId, CancellationToken ct)
    {
        var logs = await _logRepo.GetBySagaIdAsync(sagaId, ct);
        return Ok(logs);
    }
}
