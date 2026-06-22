using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollPeriods.Commands.ClosePayrollPeriod;

public sealed record ClosePayrollPeriodCommand(int PeriodId) : IRequest<Result>;

public sealed class ClosePayrollPeriodCommandHandler : IRequestHandler<ClosePayrollPeriodCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ClosePayrollPeriodCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ClosePayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _unitOfWork.PayrollPeriods.GetByIdAsync(request.PeriodId, cancellationToken);
        if (period == null)
        {
            return Result.Failure($"Payroll period with ID {request.PeriodId} not found.");
        }

        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to close payroll periods.");
        }

        if (!_currentUser.HasCompanyAccess(period.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company.");
        }

        // Rule: Cannot Close unless all runs are Posted or Locked (or Archived)
        var runs = await _unitOfWork.PayrollRuns.GetByPeriodIdAsync(period.Id, cancellationToken);
        var uncompletedRuns = runs.Where(r => r.Status != PayrollRunStatus.Posted && r.Status != PayrollRunStatus.Locked && r.Status != PayrollRunStatus.Archived).ToList();

        if (uncompletedRuns.Any())
        {
            var runCodes = string.Join(", ", uncompletedRuns.Select(r => r.RunCode));
            return Result.Failure($"Cannot close period because the following runs are not Posted or Locked: {runCodes}");
        }

        period.UpdateStatus(PayrollPeriodStatus.Closed, _currentUser.Username);
        _unitOfWork.PayrollPeriods.Update(period);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
