using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Queries;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Handlers;

public class ListPrescriptionsByAppointmentHandler : IRequestHandler<ListPrescriptionsByAppointmentQuery, List<PrescriptionResult>>
{
    private readonly IPrescriptionRepository _repo;
    public ListPrescriptionsByAppointmentHandler(IPrescriptionRepository repo) => _repo = repo;

    public async Task<List<PrescriptionResult>> Handle(ListPrescriptionsByAppointmentQuery query, CancellationToken ct)
    {
        var prescriptions = await _repo.ListByAppointmentAsync(query.AppointmentId, ct);
        return prescriptions.Select(PharmacyMapper.ToResult).ToList();
    }
}
