using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Double-entry ledger transaction debit/credit line associated with a payroll posting.
/// </summary>
public sealed class PayrollLedgerTransaction : BaseEntity
{
    public int CompanyId { get; private set; }
    public int PayrollLedgerId { get; private set; }
    public int? PayrollLedgerComponentId { get; private set; }
    public int? EmployeeId { get; private set; }
    public string AccountCode { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private PayrollLedgerTransaction() { } // For EF Core

    public static PayrollLedgerTransaction Create(
        int companyId,
        int? componentId,
        int? employeeId,
        string accountCode,
        decimal debit,
        decimal credit,
        string description)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (string.IsNullOrWhiteSpace(accountCode)) throw new ArgumentException("Account code is required.", nameof(accountCode));
        if (debit < 0 || credit < 0) throw new ArgumentException("Debit and Credit values cannot be negative.");
        if (debit == 0 && credit == 0) throw new ArgumentException("Either Debit or Credit must have a non-zero value.");

        return new PayrollLedgerTransaction
        {
            CompanyId = companyId,
            PayrollLedgerComponentId = componentId,
            EmployeeId = employeeId,
            AccountCode = accountCode,
            Debit = debit,
            Credit = credit,
            Description = description ?? string.Empty
        };
    }
}
