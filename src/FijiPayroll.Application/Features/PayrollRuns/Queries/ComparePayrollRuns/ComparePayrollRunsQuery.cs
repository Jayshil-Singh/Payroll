using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.ComparePayrollRuns;

public sealed record ComparePayrollRunsQuery(int RunAId, int RunBId) : IRequest<Result<PayrollDifferenceReport>>;

public sealed class ComparePayrollRunsQueryHandler : IRequestHandler<ComparePayrollRunsQuery, Result<PayrollDifferenceReport>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly PayrollDifferenceAnalyzer _analyzer;

    public ComparePayrollRunsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, PayrollDifferenceAnalyzer analyzer)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _analyzer = analyzer;
    }

    public async Task<Result<PayrollDifferenceReport>> Handle(ComparePayrollRunsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<PayrollDifferenceReport>.Failure("Forbidden: You do not have permission to view payroll differences.");
        }

        var runA = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(request.RunAId, cancellationToken);
        var runB = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(request.RunBId, cancellationToken);

        if (runA == null)
        {
            return Result<PayrollDifferenceReport>.Failure($"Payroll run A with ID {request.RunAId} was not found.");
        }

        if (runB == null)
        {
            return Result<PayrollDifferenceReport>.Failure($"Payroll run B with ID {request.RunBId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(runA.CompanyId) || !_currentUser.HasCompanyAccess(runB.CompanyId))
        {
            return Result<PayrollDifferenceReport>.Failure("Forbidden: You do not have access to these company's payroll runs.");
        }

        try
        {
            var report = _analyzer.CompareRuns(runA, runB);
            return Result<PayrollDifferenceReport>.Success(report);
        }
        catch (Exception ex)
        {
            return Result<PayrollDifferenceReport>.Failure($"Comparison failed: {ex.Message}");
        }
    }
}
