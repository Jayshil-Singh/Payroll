using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetExceptions;

public sealed record GetExceptionsQuery(int PayrollRunId) : IRequest<Result<IReadOnlyList<PayrollExceptionQueue>>>;

public sealed class GetExceptionsQueryHandler : IRequestHandler<GetExceptionsQuery, Result<IReadOnlyList<PayrollExceptionQueue>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetExceptionsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PayrollExceptionQueue>>> Handle(GetExceptionsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<IReadOnlyList<PayrollExceptionQueue>>.Failure("Forbidden: You do not have permission to view payroll run exceptions.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result<IReadOnlyList<PayrollExceptionQueue>>.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result<IReadOnlyList<PayrollExceptionQueue>>.Failure("Forbidden: You do not have access to this company.");
        }

        var exceptions = await _unitOfWork.PayrollExceptionQueues.GetByRunIdAsync(request.PayrollRunId, cancellationToken);
        return Result<IReadOnlyList<PayrollExceptionQueue>>.Success(exceptions);
    }
}
