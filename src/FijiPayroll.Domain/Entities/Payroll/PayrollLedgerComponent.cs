using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Breakdown details of earnings, deductions, tax, allowances, or FNPF per employee in the ledger.
/// </summary>
public sealed class PayrollLedgerComponent : BaseEntity
{
    public int PayrollLedgerEmployeeId { get; private set; }

    /// <summary>Navigation to parent ledger employee for tenant-scoped queries.</summary>
    public PayrollLedgerEmployee PayrollLedgerEmployee { get; private set; } = null!;
    public string ComponentCode { get; private set; } = string.Empty;
    public string ComponentName { get; private set; } = string.Empty;
    public ComponentType Type { get; private set; }
    public decimal Amount { get; private set; }

    private PayrollLedgerComponent() { } // For EF Core

    public static PayrollLedgerComponent Create(
        string componentCode,
        string componentName,
        ComponentType type,
        decimal amount)
    {
        if (string.IsNullOrWhiteSpace(componentCode)) throw new ArgumentException("Component code is required.", nameof(componentCode));
        if (string.IsNullOrWhiteSpace(componentName)) throw new ArgumentException("Component name is required.", nameof(componentName));

        return new PayrollLedgerComponent
        {
            ComponentCode = componentCode,
            ComponentName = componentName,
            Type = type,
            Amount = amount
        };
    }
}
