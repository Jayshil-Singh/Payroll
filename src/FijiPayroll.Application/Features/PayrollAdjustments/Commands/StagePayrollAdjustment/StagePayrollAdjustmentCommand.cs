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

namespace FijiPayroll.Application.Features.PayrollAdjustments.Commands.StagePayrollAdjustment;

public sealed record StagePayrollAdjustmentCommand(
    int CompanyId,
    int EmployeeId,
    PayrollAdjustmentType Type,
    decimal Amount,
    string Description
) : IRequest<Result<int>>;

public sealed class StagePayrollAdjustmentCommandHandler : IRequestHandler<StagePayrollAdjustmentCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public StagePayrollAdjustmentCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(StagePayrollAdjustmentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result<int>.Failure("Forbidden: You do not have permission to stage adjustments.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company.");
        }

        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<int>.Failure($"Employee with ID {request.EmployeeId} not found.");
        }

        if (employee.CompanyId != request.CompanyId)
        {
            return Result<int>.Failure("Employee does not belong to the specified company.");
        }

        var adjustment = PayrollAdjustment.Create(
            request.CompanyId,
            request.EmployeeId,
            request.Type,
            request.Amount,
            request.Description,
            _currentUser.Username
        );

        await _unitOfWork.PayrollAdjustments.AddAsync(adjustment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(adjustment.Id);
    }
}
