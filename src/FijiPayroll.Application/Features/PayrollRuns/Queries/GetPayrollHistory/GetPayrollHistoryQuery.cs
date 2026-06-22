using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollHistory;

public sealed record GetPayrollHistoryQuery(int PayrollRunId) : IRequest<Result<IReadOnlyList<PayrollRunHistory>>>;

public sealed class GetPayrollHistoryQueryHandler : IRequestHandler<GetPayrollHistoryQuery, Result<IReadOnlyList<PayrollRunHistory>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPayrollHistoryQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PayrollRunHistory>>> Handle(GetPayrollHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<IReadOnlyList<PayrollRunHistory>>.Failure("Forbidden: You do not have permission to view payroll runs.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result<IReadOnlyList<PayrollRunHistory>>.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result<IReadOnlyList<PayrollRunHistory>>.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        var history = await _unitOfWork.PayrollRunHistories.GetByRunIdAsync(request.PayrollRunId, cancellationToken);
        return Result<IReadOnlyList<PayrollRunHistory>>.Success(history);
    }
}
