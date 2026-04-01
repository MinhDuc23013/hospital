using MediatR;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Saga;

namespace PharmacyServiceDotnet.Application.Handlers;

public class CancelDispensingHandler : IRequestHandler<CancelDispensingCommand, DispensingSagaResult>
{
    private readonly DispensingSagaOrchestrator _orchestrator;

    public CancelDispensingHandler(DispensingSagaOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<DispensingSagaResult> Handle(CancelDispensingCommand cmd, CancellationToken ct)
    {
        var saga = await _orchestrator.CancelAsync(cmd.SagaId, ct);
        return PharmacyMapper.ToResult(saga);
    }
}
