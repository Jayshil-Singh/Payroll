namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Execution and submission phases of a compliance file or portal submission.
/// </summary>
public enum ComplianceSubmissionStatus
{
    /// <summary>Submission is a draft.</summary>
    Draft = 1,

    /// <summary>Submission has passed the rules engine validation.</summary>
    Validated = 2,

    /// <summary>Submission files have been compiled.</summary>
    Generated = 3,

    /// <summary>Submission has been successfully filed with the authority.</summary>
    Submitted = 4,

    /// <summary>Submission represents an amendment of a previous filing.</summary>
    Amended = 5,

    /// <summary>Submission has been superseded by a newer amendment.</summary>
    Superseded = 6,

    /// <summary>Submission data has been archived.</summary>
    Archived = 7,

    /// <summary>Submission was cancelled or discarded.</summary>
    Cancelled = 8
}
