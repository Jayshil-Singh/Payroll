using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model for staging payroll adjustments before a payroll run.
/// </summary>
public sealed class PayrollAdjustment : AuditableEntity
{
    public int CompanyId { get; private set; }
    public int EmployeeId { get; private set; }
    public PayrollAdjustmentType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsApplied { get; private set; }
    public DateTime? AppliedDate { get; private set; }
    public int? AppliedInPayrollRunId { get; private set; }
    public bool IsCancelled { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTime? CancelledDate { get; private set; }

    private PayrollAdjustment() { } // For EF Core

    public static PayrollAdjustment Create(
        int companyId,
        int employeeId,
        PayrollAdjustmentType type,
        decimal amount,
        string description,
        string createdBy)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));
        if (amount == 0) throw new ArgumentException("Adjustment amount cannot be zero.", nameof(amount));

        return new PayrollAdjustment
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            Type = type,
            Amount = amount,
            Description = description ?? string.Empty,
            IsApplied = false,
            IsCancelled = false,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Apply(int payrollRunId, string user)
    {
        if (IsApplied)
        {
            throw new InvalidOperationException("Adjustment has already been applied.");
        }
        if (IsCancelled)
        {
            throw new InvalidOperationException("Cannot apply a cancelled adjustment.");
        }

        IsApplied = true;
        AppliedDate = DateTime.UtcNow;
        AppliedInPayrollRunId = payrollRunId;
        ModifiedBy = user;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Cancel(string user)
    {
        if (IsApplied)
        {
            throw new InvalidOperationException("Cannot cancel an adjustment that has already been applied.");
        }
        if (IsCancelled)
        {
            throw new InvalidOperationException("Adjustment is already cancelled.");
        }

        IsCancelled = true;
        CancelledBy = user;
        CancelledDate = DateTime.UtcNow;
        ModifiedBy = user;
        ModifiedAt = DateTime.UtcNow;
    }
}
