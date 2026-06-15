using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// Handles <see cref="UpdateEmployeeCommand"/>.
/// </summary>
public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public UpdateEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.EmployeesEdit))
        {
            throw new ForbiddenAccessException(PermissionConstants.EmployeesEdit);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result.Failure($"You do not have access to company ID {request.CompanyId}.");
        }

        // 3. Fetch employee
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.Id, cancellationToken);
        if (employee == null)
        {
            return Result.Failure($"Employee with ID {request.Id} was not found.");
        }

        if (employee.CompanyId != request.CompanyId)
        {
            return Result.Failure("Company ID mismatch on employee update request.");
        }

        // 4. Update core properties and collection
        bool wasActive = employee.IsActive;
        try
        {
            employee.Update(
                fullName: request.FullName,
                tin: request.Tin,
                fnpfNumber: request.FnpfNumber,
                residencyStatus: request.ResidencyStatus,
                department: request.Department,
                baseSalary: request.BaseSalary,
                frequency: request.Frequency,
                isFnpfExempt: request.IsFnpfExempt,
                isTaxExempt: request.IsTaxExempt,
                isActive: request.IsActive,
                employmentType: request.EmploymentType,
                branch: request.Branch,
                position: request.Position,
                email: request.Email);

            employee.ClearPaymentMethods();
            foreach (var pmInput in request.PaymentMethods)
            {
                var pm = EmployeePaymentMethod.Create(
                    methodType: pmInput.MethodType,
                    percentage: pmInput.Percentage,
                    isPrimary: pmInput.IsPrimary,
                    bankName: pmInput.BankName,
                    bankAccountNumber: pmInput.BankAccountNumber,
                    bankSortCode: pmInput.BankSortCode,
                    mobileNumber: pmInput.MobileNumber);
                
                employee.AddPaymentMethod(pm);
            }
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation updating employee.");
            return Result.Failure(ex.Message);
        }

        // Set audit fields
        employee.ModifiedBy = _currentUser.Username;
        employee.ModifiedAt = _dateTime.UtcNow;

        if (wasActive && !employee.IsActive)
        {
            employee.AddDomainEvent(new FijiPayroll.Domain.Events.EmployeeTerminatedEvent(employee, _currentUser.Username));
        }

        employee.AddDomainEvent(new FijiPayroll.Domain.Events.EmployeeUpdatedEvent(employee));

        // 5. Save and persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee '{Name}' (ID: {Id}) updated for company {CompanyId} by {User}",
            employee.FullName, employee.Id, request.CompanyId, _currentUser.Username);

        return Result.Success();
    }
}
