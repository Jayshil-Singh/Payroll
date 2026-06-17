namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Status of a compliance batch tracking lifecycle phases.
/// </summary>
public enum ComplianceBatchStatus
{
    /// <summary>Batch is active and being worked on.</summary>
    Active = 1,

    /// <summary>Batch has been submitted to external statutory systems.</summary>
    Submitted = 2,

    /// <summary>Batch has been archived for long-term historical storage.</summary>
    Archived = 3
}
