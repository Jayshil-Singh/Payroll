using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Encapsulates workflow outcomes.
/// </summary>
public sealed record WorkflowResult(bool IsSuccess, Guid WorkflowId, string CurrentState, IReadOnlyList<string> Errors);

/// <summary>
/// Service coordinating multi-tier workflow approvals for master records.
/// </summary>
public interface IApprovalEngine
{
    /// <summary>Submits a change request for review, transitioning it to Submitted status.</summary>
    Task<WorkflowResult> SubmitAsync(string entityType, string entityId, string user, string comments);

    /// <summary>Approves a pending change request.</summary>
    Task<WorkflowResult> ApproveAsync(Guid workflowId, string user, string comments);

    /// <summary>Rejects a pending change request.</summary>
    Task<WorkflowResult> RejectAsync(Guid workflowId, string user, string comments);
}
