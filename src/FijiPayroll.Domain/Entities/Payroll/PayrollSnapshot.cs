using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Immutable snapshot storing the calculation input/output of a payroll run.
/// Guaranteed to never be updated once written.
/// </summary>
public sealed class PayrollSnapshot : BaseEntity
{
    public int CompanyId { get; private set; }
    public int PayrollRunId { get; private set; }
    public int Version { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public string JsonPayload { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedDate { get; private set; }

    private PayrollSnapshot() { } // For EF Core

    public static PayrollSnapshot Create(
        int companyId,
        int payrollRunId,
        int version,
        string jsonPayload,
        string hash,
        string createdBy)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
        if (string.IsNullOrWhiteSpace(jsonPayload)) throw new ArgumentException("Payload cannot be empty.", nameof(jsonPayload));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Hash cannot be empty.", nameof(hash));

        return new PayrollSnapshot
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            Version = version,
            JsonPayload = jsonPayload,
            Hash = hash,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow
        };
    }
}
