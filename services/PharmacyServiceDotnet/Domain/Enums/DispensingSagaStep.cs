namespace PharmacyServiceDotnet.Domain.Enums;

/// <summary>Tracks the current step of the dispensing saga orchestration.</summary>
public enum DispensingSagaStep
{
    Started = 0,
    PrescriptionCreated = 1,
    StockReserved = 2,
    PaymentCreated = 3,
    AwaitingPayment = 4,
    PaymentCompleted = 5,
    StockCommitted = 6,
    Dispensed = 7,
    Failed = 10,
    Compensating = 11,
    Compensated = 12
}
