using MediatR;
using PharmacyServiceDotnet.Application;

namespace PharmacyServiceDotnet.Application.Commands;

public record CreatePrescriptionCommand(
    Guid PatientId,
    string DoctorId,
    Guid? AppointmentId,
    string Items,
    string? Notes
) : IRequest<PrescriptionResult>;
