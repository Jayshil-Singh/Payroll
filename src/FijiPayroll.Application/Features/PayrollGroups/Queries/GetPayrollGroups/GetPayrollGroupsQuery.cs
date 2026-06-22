using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollGroups.Queries.GetPayrollGroups;

public sealed record GetPayrollGroupsQuery(int CompanyId) : IRequest<Result<IReadOnlyList<PayrollGroup>>>;

public sealed class GetPayrollGroupsQueryHandler : IRequestHandler<GetPayrollGroupsQuery, Result<IReadOnlyList<PayrollGroup>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPayrollGroupsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PayrollGroup>>> Handle(GetPayrollGroupsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<IReadOnlyList<PayrollGroup>>.Failure("Forbidden: You do not have permission to view payroll groups.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<IReadOnlyList<PayrollGroup>>.Failure("Forbidden: You do not have access to this company.");
        }

        var groups = await _unitOfWork.PayrollGroups.GetByCompanyAsync(request.CompanyId, cancellationToken);
        return Result<IReadOnlyList<PayrollGroup>>.Success(groups);
    }
}
