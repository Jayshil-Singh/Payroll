using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollPeriods.Commands.LockPayrollPeriod;

public sealed record LockPayrollPeriodCommand(int PeriodId) : IRequest<Result>;

public sealed class LockPayrollPeriodCommandHandler : IRequestHandler<LockPayrollPeriodCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public LockPayrollPeriodCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LockPayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _unitOfWork.PayrollPeriods.GetByIdAsync(request.PeriodId, cancellationToken);
        if (period == null)
        {
            return Result.Failure($"Payroll period with ID {request.PeriodId} not found.");
        }

        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to lock payroll periods.");
        }

        if (!_currentUser.HasCompanyAccess(period.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company.");
        }

        // Rule: Cannot Lock unless Closed
        if (period.Status != PayrollPeriodStatus.Closed)
        {
            return Result.Failure($"Cannot lock period because its status is '{period.Status}'. It must be Closed first.");
        }

        period.UpdateStatus(PayrollPeriodStatus.Locked, _currentUser.Username);
        _unitOfWork.PayrollPeriods.Update(period);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
