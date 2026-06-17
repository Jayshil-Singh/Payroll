namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Workflow processing states for master data change requests.
/// </summary>
public enum WorkflowState
{
    /// <summary>Draft or initial creation (pre-submission).</summary>
    Draft,

    /// <summary>Submitted for review, pending approval.</summary>
    Submitted,

    /// <summary>Pending second tier or additional supervisor action.</summary>
    Pending,

    /// <summary>Approved, changes are officially applied.</summary>
    Approved,

    /// <summary>Rejected by an approver.</summary>
    Rejected,

    /// <summary>Cancelled by the initiator.</summary>
    Cancelled
}
