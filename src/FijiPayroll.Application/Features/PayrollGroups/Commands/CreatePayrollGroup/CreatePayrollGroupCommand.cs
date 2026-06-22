using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollGroups.Commands.CreatePayrollGroup;

public sealed record CreatePayrollGroupCommand(
    int CompanyId,
    string Name,
    string Code,
    string? FilterCriteria,
    int? DefaultBankAccountId,
    int? DefaultCalendarId,
    string? DefaultCostCentre,
    int? DefaultLeaveRulesPackageId,
    int? ApprovalWorkflowId
) : IRequest<Result<int>>;

public sealed class CreatePayrollGroupCommandHandler : IRequestHandler<CreatePayrollGroupCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreatePayrollGroupCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(CreatePayrollGroupCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsCreate))
        {
            return Result<int>.Failure("Forbidden: You do not have permission to create payroll groups.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company.");
        }

        var group = PayrollGroup.Create(
            request.CompanyId,
            request.Name,
            request.Code,
            request.FilterCriteria,
            request.DefaultBankAccountId,
            request.DefaultCalendarId,
            request.DefaultCostCentre,
            request.DefaultLeaveRulesPackageId,
            request.ApprovalWorkflowId,
            _currentUser.Username
        );

        await _unitOfWork.PayrollGroups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(group.Id);
    }
}
