using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.CreatePayrollRun;

public sealed record CreatePayrollRunCommand(
    int CompanyId,
    string RunCode,
    string PeriodName,
    DateTime StartDate,
    DateTime EndDate,
    DateTime PaymentDate,
    PayrollFrequencyType Frequency,
    string? Description
) : IRequest<Result<int>>;

public sealed class CreatePayrollRunCommandHandler : IRequestHandler<CreatePayrollRunCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreatePayrollRunCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(CreatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        // Enforce permissions check
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsCreate))
        {
            return Result<int>.Failure("Forbidden: You do not have permission to create payroll runs.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company.");
        }

        var run = PayrollRun.Create(
            request.CompanyId,
            request.RunCode,
            request.PeriodName,
            request.StartDate,
            request.EndDate,
            request.PaymentDate,
            request.Frequency,
            request.Description
        );

        // IPayrollRunRepository exposed on UnitOfWork
        await _unitOfWork.PayrollRuns.AddAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(run.Id);
    }
}
