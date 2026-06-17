using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Workflows.Queries.GetPendingWorkflows;

/// <summary>
/// Query to retrieve all pending workflows for the current tenant.
/// </summary>
public sealed record GetPendingWorkflowsQuery : IRequest<List<ApprovalWorkflow>>;

/// <summary>
/// Handler for <see cref="GetPendingWorkflowsQuery"/>.
/// </summary>
public sealed class GetPendingWorkflowsQueryHandler : IRequestHandler<GetPendingWorkflowsQuery, List<ApprovalWorkflow>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes dependencies.
    /// </summary>
    public GetPendingWorkflowsQueryHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    /// <inheritdoc />
    public async Task<List<ApprovalWorkflow>> Handle(GetPendingWorkflowsQuery request, CancellationToken cancellationToken)
    {
        int companyId = _tenantProvider.GetCurrentCompanyId();
        return await _unitOfWork.Workflows.GetPendingWorkflowsAsync(companyId, cancellationToken);
    }
}
