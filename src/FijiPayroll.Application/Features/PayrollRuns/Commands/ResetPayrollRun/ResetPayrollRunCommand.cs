using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.ResetPayrollRun;

public sealed record ResetPayrollRunCommand(int PayrollRunId) : IRequest<Result>;

public sealed class ResetPayrollRunCommandHandler : IRequestHandler<ResetPayrollRunCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ResetPayrollRunCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ResetPayrollRunCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to reset payroll runs.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        try
        {
            ResetOperationContext.IsResetting = true;
            
            // Reset transition handles superseding details and updates state.
            // Reset strictly does NOT trigger recalculation or downstream workflows automatically.
            run.Reset(_currentUser.Username);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
        finally
        {
            ResetOperationContext.IsResetting = false;
        }
    }
}
