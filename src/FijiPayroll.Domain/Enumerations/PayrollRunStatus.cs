namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the lifecycle status of a payroll run.
/// </summary>
public enum PayrollRunStatus
{
    /// <summary>Run created, not yet calculated.</summary>
    Draft = 1,

    /// <summary>Calculation is actively in progress. Acts as a lock.</summary>
    Calculating = 2,

    /// <summary>Calculated successfully, pending manager review.</summary>
    Calculated = 3,

    /// <summary>Approved by an authorized manager, ready for posting.</summary>
    Approved = 4,

    /// <summary>Posted to general ledger. Financial corrections are active.</summary>
    Posted = 5,

    /// <summary>Run is closed and locked for auditing purposes.</summary>
    Locked = 6,

    /// <summary>Financial correction state applied post-posting.</summary>
    Reversed = 7
}
