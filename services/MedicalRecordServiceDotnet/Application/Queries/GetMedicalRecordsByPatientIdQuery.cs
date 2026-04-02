using MediatR;

namespace MedicalRecordServiceDotnet.Application.Queries;

public record GetMedicalRecordsByPatientIdQuery(
    string PatientId,
    int Page = 1,
    int PageSize = 50
) : IRequest<PaginatedResult<MedicalRecordResult>>;
