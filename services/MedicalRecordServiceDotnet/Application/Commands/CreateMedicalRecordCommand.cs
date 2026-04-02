using MediatR;

namespace MedicalRecordServiceDotnet.Application.Commands;

public record CreateMedicalRecordCommand(
    string PatientId,
    string AppointmentId,
    string? Findings,
    List<string>? Diagnosis,
    string? CreatedBy
) : IRequest<MedicalRecordResult>;
