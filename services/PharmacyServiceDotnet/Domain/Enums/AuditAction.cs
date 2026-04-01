namespace PharmacyServiceDotnet.Domain.Enums;

/// <summary>Types of inventory audit actions for compliance tracking.</summary>
public enum AuditAction
{
    Received = 0,
    Reserved = 1,
    Committed = 2,
    Released = 3,
    Dispensed = 4,
    Adjusted = 5,
    Expired = 6
}
