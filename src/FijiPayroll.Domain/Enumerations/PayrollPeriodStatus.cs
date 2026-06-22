namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the lifecycle status of a payroll period.
/// </summary>
public enum PayrollPeriodStatus
{
    /// <summary>Period is open for data entry and staging adjustments.</summary>
    Open = 1,

    /// <summary>Payroll calculations are currently running for this period.</summary>
    CalculationInProgress = 2,

    /// <summary>Payroll calculations have completed but not yet posted.</summary>
    Calculated = 3,

    /// <summary>Payroll calculations have been finalized and posted to ledger.</summary>
    Posted = 4,

    /// <summary>Period has been closed, preventing further changes.</summary>
    Closed = 5,

    /// <summary>Period is locked for compliance/audit, immutable.</summary>
    Locked = 6,

    /// <summary>Period is archived, read-only and backed up.</summary>
    Archived = 7
}
