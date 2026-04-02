using MediatR;

namespace MedicalRecordServiceDotnet.Application.Commands;

public record UpdateMedicalRecordCommand(
    string Id,
    string? Findings,
    List<string>? Diagnosis,
    List<LabResultInput>? LabResults
) : IRequest<MedicalRecordResult?>;

public record LabResultInput(
    string TestName,
    string? Result,
    string? NormalRange
);
