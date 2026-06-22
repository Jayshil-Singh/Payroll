using FijiPayroll.Domain.Entities.Common;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing the immutable Ledger Header for a payroll run.
/// Once finalized, it remains unmodifiable. Reversals write compensation entries.
/// </summary>
public sealed class PayrollLedger : BaseEntity
{
    public int CompanyId { get; private set; }
    public int PayrollRunId { get; private set; }
    public decimal TotalGross { get; private set; }
    public decimal TotalPAYE { get; private set; }
    public decimal TotalFNPFEmployee { get; private set; }
    public decimal TotalFNPFEmployer { get; private set; }
    public decimal TotalNetPay { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    
    public bool IsReversed { get; private set; }
    public DateTime? ReversalDate { get; private set; }
    public string? ReversalReason { get; private set; }

    private readonly List<PayrollLedgerEmployee> _employees = [];
    public IReadOnlyCollection<PayrollLedgerEmployee> Employees => _employees.AsReadOnly();

    private readonly List<PayrollLedgerTransaction> _transactions = [];
    public IReadOnlyCollection<PayrollLedgerTransaction> Transactions => _transactions.AsReadOnly();

    private PayrollLedger() { } // For EF Core

    public static PayrollLedger Create(
        int companyId,
        int payrollRunId,
        decimal totalGross,
        decimal totalPaye,
        decimal totalFnpfEmployee,
        decimal totalFnpfEmployer,
        decimal totalNetPay,
        string createdBy,
        string hash)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Integrity hash is required.", nameof(hash));

        return new PayrollLedger
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            TotalGross = totalGross,
            TotalPAYE = totalPaye,
            TotalFNPFEmployee = totalFnpfEmployee,
            TotalFNPFEmployer = totalFnpfEmployer,
            TotalNetPay = totalNetPay,
            CreatedBy = createdBy,
            CreatedUtc = DateTime.UtcNow,
            Hash = hash,
            IsReversed = false
        };
    }

    public void AddEmployee(PayrollLedgerEmployee employee)
    {
        if (employee == null) throw new ArgumentNullException(nameof(employee));
        _employees.Add(employee);
    }

    public void AddTransaction(PayrollLedgerTransaction transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        _transactions.Add(transaction);
    }

    public void Reverse(string reason, string user)
    {
        if (IsReversed)
        {
            throw new InvalidOperationException("Ledger is already reversed.");
        }

        IsReversed = true;
        ReversalDate = DateTime.UtcNow;
        ReversalReason = reason;
    }
}
