using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Queries;

public record ListPrescriptionsByAppointmentQuery(Guid AppointmentId) : IRequest<List<PrescriptionResult>>;
