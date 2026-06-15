using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.PostPayrollRun;

public sealed record PostPayrollRunCommand(int PayrollRunId) : IRequest<Result>;

public sealed class PostPayrollRunCommandHandler : IRequestHandler<PostPayrollRunCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public PostPayrollRunCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(PostPayrollRunCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsPost))
        {
            return Result.Failure("Forbidden: You do not have permission to post payroll runs.");
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
            run.Post(_currentUser.Username);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
