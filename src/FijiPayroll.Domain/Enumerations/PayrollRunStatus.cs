namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the lifecycle status of a payroll run.
/// </summary>
public enum PayrollRunStatus
{
    /// <summary>Run created, not yet calculated.</summary>
    Draft = 1,

    /// <summary>Validation is in progress before calculation.</summary>
    Validating = 8,

    /// <summary>Calculation is actively in progress. Acts as a lock.</summary>
    Calculating = 2,

    /// <summary>Calculated successfully, pending manager review.</summary>
    Calculated = 3,

    /// <summary>Approved by an authorized manager, ready for posting.</summary>
    Approved = 4,

    /// <summary>Posted to general ledger. Financial corrections are active.</summary>
    Posted = 5,

    /// <summary>Bank file has been exported.</summary>
    BankExported = 9,

    /// <summary>FRCS file has been exported.</summary>
    FrcsExported = 10,

    /// <summary>FNPF file has been exported.</summary>
    FnpfExported = 11,

    /// <summary>Evidence pack has been generated and cryptographically signed.</summary>
    EvidencePackGenerated = 12,

    /// <summary>Run is closed and locked for auditing purposes.</summary>
    Locked = 6,

    /// <summary>Run is archived and read-only.</summary>
    Archived = 13,

    /// <summary>Financial correction state applied post-posting.</summary>
    Reversed = 7
}

