using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Queries;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class GetPrescriptionHandler : IRequestHandler<GetPrescriptionQuery, PrescriptionResult?>
{
    private readonly IPrescriptionRepository _repo;
    public GetPrescriptionHandler(IPrescriptionRepository repo) => _repo = repo;

    public async Task<PrescriptionResult?> Handle(GetPrescriptionQuery query, CancellationToken ct)
    {
        var prescription = await _repo.GetByIdAsync(query.Id, ct);
        return prescription is null ? null : PharmacyMapper.ToResult(prescription);
    }
}
