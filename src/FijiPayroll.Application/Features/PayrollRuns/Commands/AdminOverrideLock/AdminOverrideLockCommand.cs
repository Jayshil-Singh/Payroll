using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.AdminOverrideLock;

public sealed record AdminOverrideLockCommand(int PayrollRunId) : IRequest<Result>;

public sealed class AdminOverrideLockCommandHandler : IRequestHandler<AdminOverrideLockCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AdminOverrideLockCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AdminOverrideLockCommand request, CancellationToken cancellationToken)
    {
        // Require Admin/Lock Override permission or Edit permission as fallback
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to override calculation locks.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
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
            // Lock Recovery: Reset the calculating lock back to draft
            run.ReleaseLockToDraft(_currentUser.Username, "Admin override lock reset execution");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
