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

namespace FijiPayroll.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// Handles <see cref="CreateEmployeeCommand"/>.
/// </summary>
public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public CreateEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.EmployeesCreate))
        {
            throw new ForbiddenAccessException(PermissionConstants.EmployeesCreate);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure($"You do not have access to company ID {request.CompanyId}.");
        }

        // 3. Create domain entity
        Employee employee;
        try
        {
            employee = Employee.Create(
                companyId: request.CompanyId,
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
            _logger.LogWarning(ex, "Domain rule violation creating employee.");
            return Result<int>.Failure(ex.Message);
        }

        // Set audit fields
        employee.CreatedBy = _currentUser.Username;
        employee.CreatedAt = _dateTime.UtcNow;

        // Raise domain event
        employee.AddDomainEvent(new FijiPayroll.Domain.Events.EmployeeCreatedEvent(employee));

        // 4. Save and persist
        await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee '{Name}' (ID: {Id}) created for company {CompanyId} by {User}",
            employee.FullName, employee.Id, request.CompanyId, _currentUser.Username);

        return Result<int>.Success(employee.Id);
    }
}
