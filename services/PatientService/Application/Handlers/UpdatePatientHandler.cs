using HospitalShared.DTOs;
using MediatR;
using PatientService.Application.Commands;
using PatientService.Domain.Exceptions;
using PatientService.Infrastructure.Repositories;
using ZiggyCreatures.Caching.Fusion;

namespace PatientService.Application.Handlers;

public class UpdatePatientHandler : IRequestHandler<UpdatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _repo;
    private readonly IFusionCache _cache;

    public UpdatePatientHandler(IPatientRepository repo, IFusionCache cache)
    {
        _repo  = repo;
        _cache = cache;
    }

    public async Task<PatientDto> Handle(UpdatePatientCommand request, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Patient", request.Id);

        patient.Update(request.FirstName, request.LastName, request.PhoneNumber);
        await _repo.SaveChangesAsync(ct);

        var dto = CreatePatientHandler.MapToDto(patient);
        // Refresh cache with updated data so subsequent reads get fresh value
        await _cache.SetAsync($"patient:{request.Id}", dto,
            new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Size = 1 }, ct);
        return dto;
    }
}
