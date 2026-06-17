using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing an immutable summary ledger entry for an employee payroll run.
/// Once written, values cannot be modified; verification is guarded by a hash.
/// </summary>
public sealed class PayrollLedger : BaseEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the associated finalized payroll run identifier.</summary>
    public int PayrollRunId { get; private set; }

    /// <summary>Gets the employee identifier.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the employee name at time of payment.</summary>
    public string EmployeeName { get; private set; } = string.Empty;

    /// <summary>Gets the employee tax identification number (TIN).</summary>
    public string EmployeeTin { get; private set; } = string.Empty;

    /// <summary>Gets the employee FNPF registration number.</summary>
    public string EmployeeFnpfNumber { get; private set; } = string.Empty;

    /// <summary>Gets the employee's gross pay.</summary>
    public decimal Gross { get; private set; }

    /// <summary>Gets the PAYE tax deducted.</summary>
    public decimal PAYE { get; private set; }

    /// <summary>Gets the FNPF employee deduction amount.</summary>
    public decimal FNPFEmployee { get; private set; }

    /// <summary>Gets the FNPF employer contribution amount.</summary>
    public decimal FNPFEmployer { get; private set; }

    /// <summary>Gets the final net pay.</summary>
    public decimal NetPay { get; private set; }

    /// <summary>Gets the currency code (e.g. FJD).</summary>
    public string Currency { get; private set; } = "FJD";

    /// <summary>Gets the SHA256 integrity hash validating ledger record values.</summary>
    public string Hash { get; private set; } = string.Empty;

    /// <summary>Gets the timestamp when the ledger record was created.</summary>
    public DateTime CreatedUtc { get; private set; }

    /// <summary>Gets the user who finalized the run and created the ledger.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    private PayrollLedger() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new immutable PayrollLedger.
    /// </summary>
    public static PayrollLedger Create(
        int companyId,
        int payrollRunId,
        int employeeId,
        string employeeName,
        string employeeTin,
        string employeeFnpfNumber,
        decimal gross,
        decimal paye,
        decimal fnpfEmployee,
        decimal fnpfEmployer,
        decimal netPay,
        string createdBy,
        string hash)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));
        if (string.IsNullOrWhiteSpace(employeeName)) throw new ArgumentException("Employee name cannot be empty.", nameof(employeeName));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Hash must be precomputed and provided.", nameof(hash));

        return new PayrollLedger
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            EmployeeTin = employeeTin,
            EmployeeFnpfNumber = employeeFnpfNumber,
            Gross = gross,
            PAYE = paye,
            FNPFEmployee = fnpfEmployee,
            FNPFEmployer = fnpfEmployer,
            NetPay = netPay,
            CreatedBy = createdBy,
            CreatedUtc = DateTime.UtcNow,
            Hash = hash
        };
    }
}
