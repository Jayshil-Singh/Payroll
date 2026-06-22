using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.RollbackPayrollRun;

public sealed record RollbackPayrollRunCommand(int PayrollRunId, string Reason) : IRequest<Result>;

public sealed class RollbackPayrollRunCommandHandler : IRequestHandler<RollbackPayrollRunCommand, Result>
{
    private readonly RollbackEngine _rollbackEngine;
    private readonly ICurrentUserService _currentUser;

    public RollbackPayrollRunCommandHandler(RollbackEngine rollbackEngine, ICurrentUserService currentUser)
    {
        _rollbackEngine = rollbackEngine;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RollbackPayrollRunCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to rollback payroll runs.");
        }

        try
        {
            await _rollbackEngine.RollbackAsync(request.PayrollRunId, request.Reason, _currentUser.Username, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Rollback failed: {ex.Message}");
        }
    }
}
