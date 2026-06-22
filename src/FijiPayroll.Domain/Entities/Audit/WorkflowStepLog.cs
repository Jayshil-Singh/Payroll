using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a workflow history audit trace step log.
/// </summary>
public sealed class WorkflowStepLog : BaseEntity
{
    private WorkflowStepLog() { }

    /// <summary>Gets the owner company ID context (multi-tenant boundary).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the unique step log entry Guid.</summary>
    public Guid LogId { get; private set; }

    /// <summary>Gets the parent workflow ID reference.</summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>Gets the state before this transition.</summary>
    public string FromState { get; private set; } = string.Empty;

    /// <summary>Gets the state after this transition.</summary>
    public string ToState { get; private set; } = string.Empty;

    /// <summary>Gets the username of the actor who performed the transition.</summary>
    public string TransitionedBy { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the transition was recorded.</summary>
    public DateTime TransitionedAt { get; private set; }

    /// <summary>Gets the optional comments documented for the transition.</summary>
    public string Comments { get; private set; } = string.Empty;

    /// <summary>
    /// Factory method to create a new step log entry.
    /// </summary>
    public static WorkflowStepLog Create(
        int companyId,
        Guid logId,
        Guid workflowId,
        string fromState,
        string toState,
        string transitionedBy,
        DateTime transitionedAt,
        string comments)
    {
        return new WorkflowStepLog
        {
            CompanyId = companyId,
            LogId = logId,
            WorkflowId = workflowId,
            FromState = fromState ?? throw new ArgumentNullException(nameof(fromState)),
            ToState = toState ?? throw new ArgumentNullException(nameof(toState)),
            TransitionedBy = transitionedBy ?? throw new ArgumentNullException(nameof(transitionedBy)),
            TransitionedAt = transitionedAt,
            Comments = comments ?? string.Empty
        };
    }
}
