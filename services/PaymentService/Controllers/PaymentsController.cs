using HospitalShared.Auth;
using HospitalShared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands;
using PaymentService.Application.Queries;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.AdminReceptionist)]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaymentAuditLogRepository _logRepo;
    private readonly IConfiguration _config;

    public PaymentsController(IMediator mediator, IPaymentAuditLogRepository logRepo, IConfiguration config)
    {
        _mediator = mediator;
        _logRepo = logRepo;
        _config = config;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(
        [FromBody] CreatePaymentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPaymentQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] Guid? appointmentId,
        [FromQuery] Guid? patientId,
        [FromQuery] PaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListPaymentsQuery(appointmentId, patientId, status, page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    public async Task<ActionResult<PaymentDto>> GetByAppointment(Guid appointmentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPaymentByAppointmentQuery(appointmentId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/process")]
    public async Task<ActionResult<PaymentDto>> Process(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ProcessPaymentCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Complete a payment after external provider confirmation (webhook/callback).</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<PaymentDto>> Complete(
        Guid id, [FromBody] CompletePaymentRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompletePaymentCommand(id, body.TransactionId), ct);
        return Ok(result);
    }

    /// <summary>Complete a CASH payment at cashier counter — one-step flow (no external provider).</summary>
    [HttpPost("{id:guid}/complete-cash")]
    public async Task<ActionResult<PaymentDto>> CompleteCash(
        Guid id, [FromBody] CompleteCashRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CompleteCashPaymentCommand(id, body.AmountReceived, body.CashierId, body.CashSessionId), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<ActionResult<PaymentDto>> Refund(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Cancel a pending/processing payment.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PaymentDto>> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelPaymentCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Get audit logs for a payment.</summary>
    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult> GetLogs(Guid id, CancellationToken ct)
    {
        var logs = await _logRepo.GetByPaymentIdAsync(id, ct);
        return Ok(logs);
    }

    /// <summary>Anonymous webhook called by external payment provider after payment confirmation.</summary>
    [AllowAnonymous]
    [HttpPost("{id:guid}/provider-webhook")]
    public async Task<ActionResult> ProviderWebhook(Guid id, [FromBody] ProviderWebhookPayload payload, CancellationToken ct)
    {
        var expectedSecret = _config["PaymentProvider:Secret"] ?? "dev-secret";
        var receivedSecret = Request.Headers["X-Provider-Secret"].FirstOrDefault();
        if (receivedSecret != expectedSecret)
            return Unauthorized("Invalid provider secret");

        var result = await _mediator.Send(new CompletePaymentCommand(id, payload.TransactionId), ct);
        return Ok(result);
    }
}

/// <summary>Request body for external payment completion.</summary>
public record CompletePaymentRequest(string TransactionId);

/// <summary>Request body for cash payment completion at cashier counter.</summary>
public record CompleteCashRequest(decimal AmountReceived, string CashierId, Guid CashSessionId);

/// <summary>Payload sent by external payment provider via webhook after payment confirmation.</summary>
public record ProviderWebhookPayload(string TransactionId);
