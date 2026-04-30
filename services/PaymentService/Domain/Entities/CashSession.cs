using HospitalShared;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Domain.Entities;

/// <summary>
/// Cashier work session — tracks opening balance, cash payments during shift,
/// and end-of-shift reconciliation (expected vs actual cash in drawer).
/// </summary>
public class CashSession
{
    public Guid Id { get; private set; }
    public string CashierId { get; private set; } = string.Empty;   // Keycloak user ID
    public string CashierName { get; private set; } = string.Empty;
    public string CounterId { get; private set; } = string.Empty;    // e.g. "Counter 1"
    public decimal OpeningBalance { get; private set; }              // Tiền đầu ca
    public decimal ExpectedCash { get; private set; }                // Opening + sum cash payments - refunds
    public decimal? ActualCash { get; private set; }                 // Cashier counted
    public decimal? Variance { get; private set; }                   // Actual - Expected
    public CashSessionStatus Status { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? Notes { get; private set; }

    private CashSession() { }

    public static CashSession Open(string cashierId, string cashierName, string counterId, decimal openingBalance)
    {
        return new CashSession
        {
            Id = GuidV7.NewGuid(),
            CashierId = cashierId,
            CashierName = cashierName,
            CounterId = counterId,
            OpeningBalance = openingBalance,
            ExpectedCash = openingBalance,
            Status = CashSessionStatus.Open,
            OpenedAt = DateTime.Now
        };
    }

    /// <summary>Add a cash payment to the session (increments ExpectedCash).</summary>
    public void AddCashIn(decimal amount)
    {
        if (Status != CashSessionStatus.Open)
            throw new DomainException("Cannot add payment to closed session.");
        ExpectedCash += amount;
    }

    /// <summary>Add a cash refund to the session (decrements ExpectedCash).</summary>
    public void AddCashOut(decimal amount)
    {
        if (Status != CashSessionStatus.Open)
            throw new DomainException("Cannot add refund to closed session.");
        ExpectedCash -= amount;
    }

    /// <summary>Close the session with actual counted cash — computes variance.</summary>
    public void Close(decimal actualCash, string? notes = null)
    {
        if (Status != CashSessionStatus.Open)
            throw new DomainException("Session already closed.");

        ActualCash = actualCash;
        Variance = actualCash - ExpectedCash;
        Notes = notes;
        Status = CashSessionStatus.Closed;
        ClosedAt = DateTime.Now;
    }
}

public enum CashSessionStatus
{
    Open = 0,
    Closed = 1,
    Reconciled = 2
}
