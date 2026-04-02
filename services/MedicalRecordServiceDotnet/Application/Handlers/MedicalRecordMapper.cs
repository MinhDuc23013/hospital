using MedicalRecordServiceDotnet.Domain.Entities;

namespace MedicalRecordServiceDotnet.Application.Handlers;

public static class MedicalRecordMapper
{
    public static MedicalRecordResult ToResult(MedicalRecord r) => new(
        r.Id, r.PatientId, r.AppointmentId, r.Findings,
        r.Diagnosis, r.LabResults.Select(ToDto).ToList(),
        r.CreatedBy, r.CreatedAt, r.UpdatedAt);

    public static LabResultDto ToDto(LabResult l) => new(
        l.TestName, l.Result, l.NormalRange, l.Timestamp);
}
