using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a workflow record for multi-tier change requests.
/// </summary>
public sealed class ApprovalWorkflow : AuditableEntity
{
    private readonly List<WorkflowStepLog> _steps = new();

    private ApprovalWorkflow() { }

    /// <summary>Gets the unique workflow request ID.</summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>Gets the entity type name associated with this workflow (e.g. Employee, PayrollRun).</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Gets the primary key/ID of the entity under workflow.</summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>Gets the current state of the workflow.</summary>
    public WorkflowState CurrentState { get; private set; }

    /// <summary>Gets the username of the user who submitted the request.</summary>
    public string RequestedBy { get; private set; } = string.Empty;

    /// <summary>Gets the roles allowed to approve the request next.</summary>
    public string CurrentApproverRole { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the workflow request was created.</summary>
    public DateTime CreatedDate { get; private set; }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the history list of step logs of the workflow.</summary>
    public IReadOnlyCollection<WorkflowStepLog> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Factory method to create a new ApprovalWorkflow.
    /// </summary>
    public static ApprovalWorkflow Create(
        Guid workflowId,
        string entityType,
        string entityId,
        string requestedBy,
        string currentApproverRole,
        int companyId)
    {
        var workflow = new ApprovalWorkflow
        {
            WorkflowId = workflowId,
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType)),
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId)),
            CurrentState = WorkflowState.Submitted,
            RequestedBy = requestedBy ?? throw new ArgumentNullException(nameof(requestedBy)),
            CurrentApproverRole = currentApproverRole ?? string.Empty,
            CreatedDate = DateTime.UtcNow,
            CompanyId = companyId
        };

        // Record initial submission step
        workflow.AddStep(
            WorkflowState.Draft.ToString(),
            WorkflowState.Submitted.ToString(),
            requestedBy,
            "Submitted for approval.");

        return workflow;
    }

    /// <summary>
    /// Adds a workflow transition log entry.
    /// </summary>
    public void AddStep(string fromState, string toState, string transitionedBy, string comments)
    {
        _steps.Add(WorkflowStepLog.Create(
            Guid.NewGuid(),
            WorkflowId,
            fromState,
            toState,
            transitionedBy,
            DateTime.UtcNow,
            comments
        ));
    }

    /// <summary>
    /// Transition the workflow to a new state and record the step.
    /// </summary>
    public void TransitionTo(WorkflowState newState, string transitionedBy, string comments, string nextApproverRole = "")
    {
        var oldState = CurrentState;
        CurrentState = newState;
        CurrentApproverRole = nextApproverRole;

        AddStep(oldState.ToString(), newState.ToString(), transitionedBy, comments);
    }
}
