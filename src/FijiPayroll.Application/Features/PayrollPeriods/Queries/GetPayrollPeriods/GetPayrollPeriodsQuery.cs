using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollPeriods.Queries.GetPayrollPeriods;

public sealed record GetPayrollPeriodsQuery(int CompanyId) : IRequest<Result<IReadOnlyList<PayrollPeriod>>>;

public sealed class GetPayrollPeriodsQueryHandler : IRequestHandler<GetPayrollPeriodsQuery, Result<IReadOnlyList<PayrollPeriod>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPayrollPeriodsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PayrollPeriod>>> Handle(GetPayrollPeriodsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<IReadOnlyList<PayrollPeriod>>.Failure("Forbidden: You do not have permission to view payroll periods.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<IReadOnlyList<PayrollPeriod>>.Failure("Forbidden: You do not have access to this company.");
        }

        var periods = await _unitOfWork.PayrollPeriods.GetByCompanyAsync(request.CompanyId, cancellationToken);
        return Result<IReadOnlyList<PayrollPeriod>>.Success(periods);
    }
}
