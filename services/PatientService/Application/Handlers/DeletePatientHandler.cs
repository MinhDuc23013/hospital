using MediatR;
using PatientService.Application.Commands;
using PatientService.Domain.Exceptions;
using PatientService.Infrastructure.Repositories;
using ZiggyCreatures.Caching.Fusion;

namespace PatientService.Application.Handlers;

public class DeletePatientHandler : IRequestHandler<DeletePatientCommand>
{
    private readonly IPatientRepository _repo;
    private readonly IFusionCache _cache;

    public DeletePatientHandler(IPatientRepository repo, IFusionCache cache)
    {
        _repo  = repo;
        _cache = cache;
    }

    public async Task Handle(DeletePatientCommand request, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Patient", request.Id);

        patient.Deactivate();
        await _repo.SaveChangesAsync(ct);

        // Evict cache so subsequent reads fetch the deactivated state from DB
        await _cache.RemoveAsync($"patient:{request.Id}", token: ct);
    }
}
