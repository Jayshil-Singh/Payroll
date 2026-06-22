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

namespace FijiPayroll.Application.Features.PayrollPeriods.Commands.CreatePayrollPeriod;

public sealed record CreatePayrollPeriodCommand(
    int CompanyId,
    string PeriodCode,
    PayrollFrequencyType Frequency,
    int FiscalYear,
    int FiscalMonth,
    DateTime StartDate,
    DateTime EndDate,
    DateTime PaymentDate
) : IRequest<Result<int>>;

public sealed class CreatePayrollPeriodCommandHandler : IRequestHandler<CreatePayrollPeriodCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreatePayrollPeriodCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(CreatePayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsCreate))
        {
            return Result<int>.Failure("Forbidden: You do not have permission to create payroll periods.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company.");
        }

        // Rule: Only ONE Open period per Company per Frequency.
        var existingOpen = await _unitOfWork.PayrollPeriods.GetOpenPeriodAsync(request.CompanyId, request.Frequency, cancellationToken);
        if (existingOpen != null)
        {
            return Result<int>.Failure($"An open period for frequency {request.Frequency} already exists for this company.");
        }

        var period = PayrollPeriod.Create(
            request.CompanyId,
            request.PeriodCode,
            request.Frequency,
            request.FiscalYear,
            request.FiscalMonth,
            request.StartDate,
            request.EndDate,
            request.PaymentDate,
            _currentUser.Username
        );

        await _unitOfWork.PayrollPeriods.AddAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(period.Id);
    }
}
