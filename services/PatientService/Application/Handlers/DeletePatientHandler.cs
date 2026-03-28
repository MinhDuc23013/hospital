using MediatR;
using PatientService.Application.Commands;
using PatientService.Domain.Exceptions;
using PatientService.Infrastructure.Repositories;

namespace PatientService.Application.Handlers;

public class DeletePatientHandler : IRequestHandler<DeletePatientCommand>
{
    private readonly IPatientRepository _repo;

    public DeletePatientHandler(IPatientRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(DeletePatientCommand request, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Patient", request.Id);

        patient.Deactivate();
        await _repo.SaveChangesAsync(ct);
    }
}
