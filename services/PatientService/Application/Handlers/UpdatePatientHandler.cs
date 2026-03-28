using HospitalShared.DTOs;
using MediatR;
using PatientService.Application.Commands;
using PatientService.Domain.Exceptions;
using PatientService.Infrastructure.Repositories;

namespace PatientService.Application.Handlers;

public class UpdatePatientHandler : IRequestHandler<UpdatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _repo;

    public UpdatePatientHandler(IPatientRepository repo)
    {
        _repo = repo;
    }

    public async Task<PatientDto> Handle(UpdatePatientCommand request, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Patient", request.Id);

        patient.Update(request.FirstName, request.LastName, request.PhoneNumber);
        await _repo.SaveChangesAsync(ct);

        return CreatePatientHandler.MapToDto(patient);
    }
}
