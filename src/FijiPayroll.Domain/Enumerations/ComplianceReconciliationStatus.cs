namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// States for audit reconciliation variances.
/// </summary>
public enum ComplianceReconciliationStatus
{
    /// <summary>Ledger amounts and compliance export amounts reconcile perfectly.</summary>
    Balanced = 1,

    /// <summary>Variance identified within tolerable warning limits.</summary>
    Warning = 2,

    /// <summary>Critical variance identified requiring system override or rollback.</summary>
    Critical = 3
}
