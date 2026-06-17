using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Services;

/// <summary>
/// Implementation of IApprovalEngine that manages ApprovalWorkflow aggregate transitions.
/// </summary>
public sealed class ApprovalEngine : IApprovalEngine
{
    private readonly ApplicationDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes dependencies.
    /// </summary>
    public ApprovalEngine(
        ApplicationDbContext context,
        IEventBus eventBus,
        ITenantProvider tenantProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    /// <inheritdoc />
    public async Task<WorkflowResult> SubmitAsync(string entityType, string entityId, string user, string comments)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentNullException(nameof(entityType));
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentNullException(nameof(entityId));

        int companyId = _tenantProvider.GetCurrentCompanyId();

        // Find if any active workflow already exists for this entity to prevent duplicate workflows
        var existing = await _context.ApprovalWorkflows
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.CompanyId == companyId &&
                                      w.EntityType == entityType &&
                                      w.EntityId == entityId &&
                                      (w.CurrentState == WorkflowState.Submitted || w.CurrentState == WorkflowState.Pending));

        if (existing != null)
        {
            return new WorkflowResult(
                false,
                existing.WorkflowId,
                existing.CurrentState.ToString(),
                new[] { "A workflow request is already active for this entity." });
        }

        string defaultApproverRole = entityType.Equals("PayrollRun", StringComparison.OrdinalIgnoreCase) ? "Manager" : "Supervisor";
        var workflowId = Guid.NewGuid();
        var workflow = ApprovalWorkflow.Create(workflowId, entityType, entityId, user, defaultApproverRole, companyId);

        workflow.CreatedBy = user;
        workflow.CreatedAt = DateTime.UtcNow;

        await _context.ApprovalWorkflows.AddAsync(workflow);
        await _context.SaveChangesAsync();

        // Publish event
        await _eventBus.PublishAsync(new WorkflowStateChangedEvent(
            workflowId,
            entityType,
            entityId,
            WorkflowState.Draft,
            WorkflowState.Submitted,
            user,
            comments));

        return new WorkflowResult(true, workflowId, workflow.CurrentState.ToString(), Array.Empty<string>());
    }

    /// <inheritdoc />
    public async Task<WorkflowResult> ApproveAsync(Guid workflowId, string user, string comments)
    {
        int companyId = _tenantProvider.GetCurrentCompanyId();

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId && w.CompanyId == companyId);

        if (workflow == null)
        {
            return new WorkflowResult(false, workflowId, string.Empty, new[] { "Workflow request was not found." });
        }

        if (workflow.CurrentState != WorkflowState.Submitted && workflow.CurrentState != WorkflowState.Pending)
        {
            return new WorkflowResult(
                false,
                workflowId,
                workflow.CurrentState.ToString(),
                new[] { "Workflow request is not in a state that can be approved." });
        }

        var oldState = workflow.CurrentState;
        workflow.TransitionTo(WorkflowState.Approved, user, comments);

        workflow.ModifiedBy = user;
        workflow.ModifiedAt = DateTime.UtcNow;

        _context.ApprovalWorkflows.Update(workflow);
        await _context.SaveChangesAsync();

        // Publish event
        await _eventBus.PublishAsync(new WorkflowStateChangedEvent(
            workflowId,
            workflow.EntityType,
            workflow.EntityId,
            oldState,
            WorkflowState.Approved,
            user,
            comments));

        return new WorkflowResult(true, workflowId, workflow.CurrentState.ToString(), Array.Empty<string>());
    }

    /// <inheritdoc />
    public async Task<WorkflowResult> RejectAsync(Guid workflowId, string user, string comments)
    {
        int companyId = _tenantProvider.GetCurrentCompanyId();

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId && w.CompanyId == companyId);

        if (workflow == null)
        {
            return new WorkflowResult(false, workflowId, string.Empty, new[] { "Workflow request was not found." });
        }

        if (workflow.CurrentState != WorkflowState.Submitted && workflow.CurrentState != WorkflowState.Pending)
        {
            return new WorkflowResult(
                false,
                workflowId,
                workflow.CurrentState.ToString(),
                new[] { "Workflow request is not in a state that can be rejected." });
        }

        var oldState = workflow.CurrentState;
        workflow.TransitionTo(WorkflowState.Rejected, user, comments);

        workflow.ModifiedBy = user;
        workflow.ModifiedAt = DateTime.UtcNow;

        _context.ApprovalWorkflows.Update(workflow);
        await _context.SaveChangesAsync();

        // Publish event
        await _eventBus.PublishAsync(new WorkflowStateChangedEvent(
            workflowId,
            workflow.EntityType,
            workflow.EntityId,
            oldState,
            WorkflowState.Rejected,
            user,
            comments));

        return new WorkflowResult(true, workflowId, workflow.CurrentState.ToString(), Array.Empty<string>());
    }
}
