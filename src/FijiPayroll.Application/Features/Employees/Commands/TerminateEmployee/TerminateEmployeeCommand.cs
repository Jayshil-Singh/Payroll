using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Employees.Commands.TerminateEmployee;

/// <summary>
/// Command to terminate (deactivate) an employee, deactivate payment methods, and raise domain events.
/// </summary>
public sealed record TerminateEmployeeCommand(int EmployeeId) : IRequest<Result>, ITransactional;

/// <summary>
/// Handler for <see cref="TerminateEmployeeCommand"/>.
/// </summary>
public sealed class TerminateEmployeeCommandHandler : IRequestHandler<TerminateEmployeeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<TerminateEmployeeCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public TerminateEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<TerminateEmployeeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(TerminateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.EmployeesTerminate))
        {
            throw new ForbiddenAccessException(PermissionConstants.EmployeesTerminate);
        }

        // 2. Load employee
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result.Failure($"Employee with ID {request.EmployeeId} was not found.");
        }

        // 3. Company access check
        if (!_currentUser.HasCompanyAccess(employee.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company's records.");
        }

        // 4. Check if already terminated
        if (!employee.IsActive)
        {
            return Result.Failure("Employee is already inactive/terminated.");
        }

        // 5. Terminate employee
        try
        {
            employee.Terminate(_currentUser.Username);
            employee.ModifiedBy = _currentUser.Username;
            employee.ModifiedAt = _dateTime.UtcNow;
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation terminating employee ID {Id}.", request.EmployeeId);
            return Result.Failure(ex.Message);
        }

        // 6. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee '{Name}' (ID: {Id}) was successfully terminated by {User}.",
            employee.FullName, employee.Id, _currentUser.Username);

        return Result.Success();
    }
}
