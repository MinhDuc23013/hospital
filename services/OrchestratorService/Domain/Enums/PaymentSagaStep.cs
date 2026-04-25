namespace OrchestratorService.Domain.Enums;

/// <summary>Tracks the current step of the payment saga orchestration.</summary>
public enum PaymentSagaStep
{
    Initiated = 0,
    Processing = 1,   // Payment created in PaymentService, checkout URL obtained
    Completed = 5,
    Failed = 6
}
