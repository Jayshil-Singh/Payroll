using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Domain event raised when a workflow request's state is changed.
/// </summary>
public sealed class WorkflowStateChangedEvent : IDomainEvent
{
    /// <summary>Gets the unique workflow request ID.</summary>
    public Guid WorkflowId { get; }

    /// <summary>Gets the entity type associated with the workflow (e.g. Employee, PayrollRun).</summary>
    public string EntityType { get; }

    /// <summary>Gets the unique ID of the entity under workflow.</summary>
    public string EntityId { get; }

    /// <summary>Gets the previous workflow state.</summary>
    public WorkflowState OldState { get; }

    /// <summary>Gets the new workflow state.</summary>
    public WorkflowState NewState { get; }

    /// <summary>Gets the username of the user who performed the transition.</summary>
    public string TransitionedBy { get; }

    /// <summary>Gets custom comments recorded for this transition.</summary>
    public string Comments { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initialises a new instance of the event.
    /// </summary>
    public WorkflowStateChangedEvent(
        Guid workflowId,
        string entityType,
        string entityId,
        WorkflowState oldState,
        WorkflowState newState,
        string transitionedBy,
        string comments)
    {
        WorkflowId = workflowId;
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
        OldState = oldState;
        NewState = newState;
        TransitionedBy = transitionedBy ?? "System";
        Comments = comments ?? string.Empty;
        OccurredOn = DateTime.UtcNow;
    }
}
