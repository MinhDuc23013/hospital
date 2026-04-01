using MediatR;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Application.Saga;

namespace PharmacyServiceDotnet.Application.Handlers;

public class StartDispensingHandler : IRequestHandler<StartDispensingCommand, DispensingSagaResult>
{
    private readonly DispensingSagaOrchestrator _orchestrator;

    public StartDispensingHandler(DispensingSagaOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<DispensingSagaResult> Handle(StartDispensingCommand cmd, CancellationToken ct)
    {
        var saga = await _orchestrator.StartAsync(
            cmd.PatientId, cmd.DoctorId, cmd.AppointmentId,
            cmd.Items, cmd.PaymentAmount, cmd.Notes, ct);
        return PharmacyMapper.ToResult(saga);
    }
}
