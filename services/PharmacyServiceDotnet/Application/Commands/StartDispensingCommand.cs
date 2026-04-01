using MediatR;

namespace PharmacyServiceDotnet.Application.Commands;

/// <summary>
/// Initiates the dispensing saga: creates prescription → reserves stock (FEFO) → awaits payment.
/// </summary>
public record StartDispensingCommand(
    Guid PatientId,
    string DoctorId,
    Guid? AppointmentId,
    string Items,          // JSON array of [{drugId, drugName, quantity, dosage}]
    decimal PaymentAmount,
    string? Notes = null
) : IRequest<DispensingSagaResult>;
