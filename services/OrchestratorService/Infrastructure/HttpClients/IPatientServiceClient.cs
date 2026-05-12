using HospitalShared.DTOs;

namespace OrchestratorService.Infrastructure.HttpClients;

public interface IPatientServiceClient
{
    Task<PatientDto?> GetPatientAsync(Guid patientId, CancellationToken ct = default);
}
