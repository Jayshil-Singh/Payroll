using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// EF Core repository implementation for the <see cref="ApprovalWorkflow"/> entity.
/// </summary>
public sealed class ApprovalWorkflowRepository : IApprovalWorkflowRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes with DbContext.
    /// </summary>
    public ApprovalWorkflowRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken)
    {
        await _context.Set<ApprovalWorkflow>().AddAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(ApprovalWorkflow workflow)
    {
        _context.Set<ApprovalWorkflow>().Update(workflow);
    }

    /// <inheritdoc />
    public async Task<ApprovalWorkflow?> GetByIdAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        return await _context.Set<ApprovalWorkflow>()
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.WorkflowId == workflowId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ApprovalWorkflow>> GetPendingWorkflowsAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Set<ApprovalWorkflow>()
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId &&
                        (x.CurrentState == WorkflowState.Submitted || x.CurrentState == WorkflowState.Pending))
            .ToListAsync(cancellationToken);
    }
}
