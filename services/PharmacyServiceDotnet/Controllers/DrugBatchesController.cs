using HospitalShared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Handlers;
using PharmacyServiceDotnet.Domain.Exceptions;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Controllers;

[ApiController]
[Route("api/drugs/{drugId:guid}/batches")]
[Authorize(Roles = Roles.AdminPharmacist)]
public class DrugBatchesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDrugBatchRepository _batchRepo;
    private readonly IDrugRepository _drugRepo;

    public DrugBatchesController(IMediator mediator, IDrugBatchRepository batchRepo, IDrugRepository drugRepo)
    {
        _mediator = mediator;
        _batchRepo = batchRepo;
        _drugRepo = drugRepo;
    }

    [HttpPost]
    public async Task<ActionResult<DrugBatchResult>> Create(
        Guid drugId, [FromBody] CreateDrugBatchRequest request, CancellationToken ct)
    {
        var command = new CreateDrugBatchCommand(drugId, request.BatchNumber, request.ExpiryDate, request.Quantity, request.ReceivedDate);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(ListByDrug), new { drugId }, result);
    }

    [HttpGet]
    public async Task<ActionResult> ListByDrug(Guid drugId, CancellationToken ct)
    {
        _ = await _drugRepo.GetByIdAsync(drugId, ct)
            ?? throw new NotFoundException("Drug", drugId);

        var batches = await _batchRepo.GetByDrugIdAsync(drugId, ct);
        return Ok(new { data = batches.Select(PharmacyMapper.ToResult) });
    }
}

public record CreateDrugBatchRequest(
    string BatchNumber,
    DateTime ExpiryDate,
    int Quantity,
    DateTime? ReceivedDate = null
);
