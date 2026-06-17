using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a generated Bank Credit clearing file.
/// </summary>
public sealed class BankFile : AuditableEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the unique bank code (e.g. "BSP", "ANZ", "WBC").</summary>
    public string BankCode { get; private set; } = string.Empty;

    /// <summary>Gets the source payroll run identifier.</summary>
    public int PayrollRunId { get; private set; }

    /// <summary>Gets the total currency amount disbursed in the file.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Gets the count of distinct employee payouts included.</summary>
    public int TotalEmployeesCount { get; private set; }

    /// <summary>Gets the formatted raw file text body.</summary>
    public string FileContent { get; private set; } = string.Empty;

    /// <summary>Gets the output file path on the system filesystem.</summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>Gets the integrity hash validating the return file content.</summary>
    public string Hash { get; private set; } = string.Empty;

    private BankFile() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new BankFile.
    /// </summary>
    public static BankFile Create(
        int companyId,
        string bankCode,
        int payrollRunId,
        decimal totalAmount,
        int totalEmployeesCount,
        string fileContent,
        string filePath,
        string hash)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (string.IsNullOrWhiteSpace(bankCode)) throw new ArgumentException("Bank code cannot be empty.", nameof(bankCode));
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (totalAmount <= 0) throw new ArgumentException("Total amount must be greater than zero.", nameof(totalAmount));
        if (totalEmployeesCount <= 0) throw new ArgumentException("Total employees count must be greater than zero.", nameof(totalEmployeesCount));
        if (string.IsNullOrWhiteSpace(fileContent)) throw new ArgumentException("File content cannot be empty.", nameof(fileContent));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Hash cannot be empty.", nameof(hash));

        return new BankFile
        {
            CompanyId = companyId,
            BankCode = bankCode,
            PayrollRunId = payrollRunId,
            TotalAmount = totalAmount,
            TotalEmployeesCount = totalEmployeesCount,
            FileContent = fileContent,
            FilePath = filePath,
            Hash = hash
        };
    }
}
