using HospitalShared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Controllers;

/// <summary>Cashier session management — open/close shift + complete cash payments.</summary>
[ApiController]
[Route("api/cash-sessions")]
[Authorize(Roles = Roles.AdminReceptionist)]
public class CashSessionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICashSessionRepository _repo;

    public CashSessionsController(IMediator mediator, ICashSessionRepository repo)
    {
        _mediator = mediator;
        _repo = repo;
    }

    /// <summary>Open a new shift. Returns the current open session if cashier already has one.</summary>
    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenCashSessionCommand cmd, CancellationToken ct)
    {
        var session = await _mediator.Send(cmd, ct);
        return Ok(session);
    }

    /// <summary>Close shift — report actual counted cash. System computes variance.</summary>
    [HttpPost("{sessionId:guid}/close")]
    public async Task<IActionResult> Close(
        Guid sessionId,
        [FromBody] CloseCashSessionRequest body,
        CancellationToken ct)
    {
        var session = await _mediator.Send(new CloseCashSessionCommand(sessionId, body.ActualCash, body.Notes), ct);
        return Ok(session);
    }

    /// <summary>Get session by ID.</summary>
    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> Get(Guid sessionId, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(sessionId, ct);
        return session is null ? NotFound() : Ok(session);
    }

    /// <summary>Get current open session for a cashier.</summary>
    [HttpGet("current/{cashierId}")]
    public async Task<IActionResult> GetCurrent(string cashierId, CancellationToken ct)
    {
        var session = await _repo.GetOpenSessionByCashierAsync(cashierId, ct);
        return session is null ? NotFound() : Ok(session);
    }
}

public record CloseCashSessionRequest(decimal ActualCash, string? Notes);
