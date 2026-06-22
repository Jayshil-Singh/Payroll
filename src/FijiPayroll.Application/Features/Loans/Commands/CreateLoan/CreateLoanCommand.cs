using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Loans.Commands.CreateLoan;

/// <summary>
/// Command to register/create a new employee staff loan.
/// </summary>
public sealed record CreateLoanCommand(
    int CompanyId,
    int EmployeeId,
    string Description,
    decimal PrincipalAmount,
    decimal InterestRate,
    decimal DeductionAmountPerPeriod,
    DateTime StartDate
) : IRequest<Result<int>>, ITransactional;

/// <summary>
/// Handler for <see cref="CreateLoanCommand"/>.
/// </summary>
public sealed class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CreateLoanCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public CreateLoanCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<CreateLoanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LoansCreate))
        {
            throw new ForbiddenAccessException(PermissionConstants.LoansCreate);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company.");
        }

        // 3. Load employee to make sure they exist and belong to this company
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<int>.Failure($"Employee with ID {request.EmployeeId} was not found.");
        }

        if (employee.CompanyId != request.CompanyId)
        {
            return Result<int>.Failure("Employee company mismatch.");
        }

        // 4. Create and initialize Loan entity
        Loan loan;
        try
        {
            loan = Loan.Create(
                companyId: request.CompanyId,
                employeeId: request.EmployeeId,
                description: request.Description,
                principal: request.PrincipalAmount,
                interestRate: request.InterestRate,
                deductionPerPeriod: request.DeductionAmountPerPeriod,
                startDate: request.StartDate);

            loan.CreatedBy = _currentUser.Username;
            loan.CreatedAt = _dateTime.UtcNow;
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation creating loan.");
            return Result<int>.Failure(ex.Message);
        }

        // 5. Persist loan
        await _unitOfWork.Loans.AddAsync(loan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Loan (ID: {LoanId}) of {Principal} registered for employee {EmployeeId} by {User}",
            loan.Id, request.PrincipalAmount, request.EmployeeId, _currentUser.Username);

        return Result<int>.Success(loan.Id);
    }
}
