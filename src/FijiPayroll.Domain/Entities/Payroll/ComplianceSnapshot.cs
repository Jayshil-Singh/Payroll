using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model capturing an immutable snapshot of raw payroll run details 
/// serialized as JSON, used for auditing and amendment comparisons.
/// </summary>
public sealed class ComplianceSnapshot : BaseEntity
{
    /// <summary>Gets the associated batch identifier if part of a submitted file.</summary>
    public int? ComplianceBatchId { get; private set; }

    /// <summary>Gets the source payroll run identifier.</summary>
    public int PayrollRunId { get; private set; }

    /// <summary>Gets the snapshot version descriptor (e.g. "v1", "v2").</summary>
    public string SnapshotVersion { get; private set; } = string.Empty;

    /// <summary>Gets the raw serialized payroll data payload.</summary>
    public string SnapshotJson { get; private set; } = string.Empty;

    /// <summary>Gets the integrity hash of the raw JSON to check tamper state.</summary>
    public string SHA256Hash { get; private set; } = string.Empty;

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedUtc { get; private set; }

    /// <summary>Gets the user who triggered the snapshot generation.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    private ComplianceSnapshot() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new ComplianceSnapshot.
    /// </summary>
    public static ComplianceSnapshot Create(
        int? complianceBatchId,
        int payrollRunId,
        string snapshotVersion,
        string snapshotJson,
        string sha256Hash,
        string createdBy)
    {
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (string.IsNullOrWhiteSpace(snapshotVersion)) throw new ArgumentException("Snapshot version cannot be empty.", nameof(snapshotVersion));
        if (string.IsNullOrWhiteSpace(snapshotJson)) throw new ArgumentException("Snapshot JSON cannot be empty.", nameof(snapshotJson));
        if (string.IsNullOrWhiteSpace(sha256Hash)) throw new ArgumentException("Hash code cannot be empty.", nameof(sha256Hash));

        return new ComplianceSnapshot
        {
            ComplianceBatchId = complianceBatchId,
            PayrollRunId = payrollRunId,
            SnapshotVersion = snapshotVersion,
            SnapshotJson = snapshotJson,
            SHA256Hash = sha256Hash,
            CreatedBy = createdBy,
            CreatedUtc = DateTime.UtcNow
        };
    }
}
