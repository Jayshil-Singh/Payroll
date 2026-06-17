using FijiPayroll.Domain.Entities.Audit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository abstraction for ApprovalWorkflow aggregate.
/// </summary>
public interface IApprovalWorkflowRepository
{
    /// <summary>Adds a new workflow record.</summary>
    Task AddAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken);

    /// <summary>Updates an existing workflow record.</summary>
    void Update(ApprovalWorkflow workflow);

    /// <summary>Gets a workflow by its unique ID.</summary>
    Task<ApprovalWorkflow?> GetByIdAsync(Guid workflowId, CancellationToken cancellationToken);

    /// <summary>Gets all pending workflows (Submitted/Pending states) for a company.</summary>
    Task<List<ApprovalWorkflow>> GetPendingWorkflowsAsync(int companyId, CancellationToken cancellationToken);
}
