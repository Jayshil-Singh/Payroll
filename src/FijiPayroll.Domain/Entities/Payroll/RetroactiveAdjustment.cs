using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain entity representing a retroactive adjustment (e.g. missed overtime, salary correction)
/// applied in a subsequent pay run.
/// </summary>
public sealed class RetroactiveAdjustment : AuditableEntity
{
    private string _componentName = string.Empty;
    private string _description = string.Empty;

    private RetroactiveAdjustment() { }

    /// <summary>Gets the company ID context (multi-tenant boundary).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the employee ID target.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the monetary amount.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the component type (Earning, Deduction, Allowance, Overtime, etc.).</summary>
    public ComponentType ComponentType { get; private set; }

    /// <summary>Gets the component name.</summary>
    public string ComponentName
    {
        get => _componentName;
        private set => _componentName = value ?? string.Empty;
    }

    /// <summary>Gets the detailed correction description.</summary>
    public string Description
    {
        get => _description;
        private set => _description = value ?? string.Empty;
    }

    /// <summary>Gets whether the adjustment has been applied in a payroll run.</summary>
    public bool IsApplied { get; private set; }

    /// <summary>Gets the payroll run ID where this adjustment was applied.</summary>
    public int? AppliedInPayrollRunId { get; private set; }

    /// <summary>Gets the date when it was marked as applied.</summary>
    public DateTime? AppliedAt { get; private set; }

    /// <summary>
    /// Factory method to construct a new RetroactiveAdjustment.
    /// </summary>
    public static RetroactiveAdjustment Create(
        int companyId,
        int employeeId,
        decimal amount,
        ComponentType componentType,
        string componentName,
        string description)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));
        if (string.IsNullOrWhiteSpace(componentName)) throw new ArgumentNullException(nameof(componentName));

        return new RetroactiveAdjustment
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            Amount = amount,
            ComponentType = componentType,
            ComponentName = componentName,
            Description = description ?? string.Empty,
            IsApplied = false
        };
    }

    /// <summary>
    /// Marks the adjustment as applied in the specified payroll run.
    /// </summary>
    public void MarkAsApplied(int payrollRunId)
    {
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        IsApplied = true;
        AppliedInPayrollRunId = payrollRunId;
        AppliedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unapplies/reverts the applied status (e.g. during a run reset or reversal).
    /// </summary>
    public void RevertApplication()
    {
        IsApplied = false;
        AppliedInPayrollRunId = null;
        AppliedAt = null;
    }
}
