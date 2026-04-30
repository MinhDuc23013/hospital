using HospitalShared;
using LabTestService.Domain.Enums;

namespace LabTestService.Domain.Entities;

/// <summary>A single test item within a lab order (e.g. CBC, glucose, etc.).</summary>
public class LabOrderItem
{
    public Guid Id { get; private set; }
    public Guid LabOrderId { get; private set; }
    public string TestName { get; private set; } = string.Empty;
    public string TestCode { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string? Result { get; private set; }
    public string? NormalRange { get; private set; }
    public string? Unit { get; private set; }
    public LabResultStatus ResultStatus { get; private set; }
    public DateTime? ResultedAt { get; private set; }

    private LabOrderItem() { }

    public decimal UnitPrice { get; private set; }

    public static LabOrderItem Create(Guid labOrderId, string testName, string testCode, string category, decimal unitPrice = 0) =>
        new()
        {
            Id = GuidV7.NewGuid(),
            LabOrderId = labOrderId,
            TestName = testName,
            TestCode = testCode,
            Category = category,
            UnitPrice = unitPrice,
            ResultStatus = LabResultStatus.Pending
        };

    public void SubmitResult(string result, string? normalRange, string? unit, LabResultStatus status)
    {
        Result = result;
        NormalRange = normalRange;
        Unit = unit;
        ResultStatus = status;
        ResultedAt = DateTime.UtcNow;
    }
}
