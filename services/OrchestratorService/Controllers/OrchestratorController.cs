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
    private readonly IPaymentSagaRepository _paymentSagaRepo;

    public OrchestratorController(
        IMediator mediator,
        IBookingSagaLogRepository logRepo,
        IPaymentSagaRepository paymentSagaRepo)
    {
        _mediator = mediator;
        _logRepo = logRepo;
        _paymentSagaRepo = paymentSagaRepo;
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
            "SlotReserved" => Accepted($"/api/orchestrator/book/{result.SagaId}/logs", result),
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

    /// <summary>
    /// Initiate payment saga for an appointment.
    /// Sync phase: validate invoice → create payment → process payment (get checkout URL).
    /// Async phase: PaymentEventConsumer listens for provider outcome and updates saga state.
    /// Returns 202 with checkout URL on success, or 422 if payment creation failed.
    /// </summary>
    [HttpPost("payment/initiate")]
    public async Task<ActionResult<PaymentSagaResult>> InitiatePayment(
        [FromBody] InitiatePaymentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.Status switch
        {
            "Processing" => Accepted((string?)null, result),
            "Completed"  => Ok(result),
            _ => UnprocessableEntity(result)
        };
    }

    /// <summary>Get payment saga status by saga ID.</summary>
    [HttpGet("payment/{sagaId:guid}")]
    public async Task<ActionResult> GetPaymentSaga(Guid sagaId, CancellationToken ct)
    {
        var saga = await _paymentSagaRepo.GetByIdAsync(sagaId, ct);
        return saga is null ? NotFound() : Ok(saga);
    }
}
