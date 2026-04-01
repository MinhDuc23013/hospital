using MediatR;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Saga;

namespace PharmacyServiceDotnet.Application.Handlers;

public class CompleteDispensingPaymentHandler : IRequestHandler<CompleteDispensingPaymentCommand, DispensingSagaResult>
{
    private readonly DispensingSagaOrchestrator _orchestrator;

    public CompleteDispensingPaymentHandler(DispensingSagaOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<DispensingSagaResult> Handle(CompleteDispensingPaymentCommand cmd, CancellationToken ct)
    {
        var saga = await _orchestrator.CompletePaymentAsync(cmd.SagaId, ct);
        return PharmacyMapper.ToResult(saga);
    }
}
