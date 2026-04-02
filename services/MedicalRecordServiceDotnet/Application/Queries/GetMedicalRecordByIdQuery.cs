using MediatR;

namespace MedicalRecordServiceDotnet.Application.Queries;

public record GetMedicalRecordByIdQuery(string Id) : IRequest<MedicalRecordResult?>;
