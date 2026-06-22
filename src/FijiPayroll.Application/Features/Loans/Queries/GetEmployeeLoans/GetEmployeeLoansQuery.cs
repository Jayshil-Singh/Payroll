using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Loans.Queries.GetEmployeeLoans;

/// <summary>
/// Query to retrieve active and historical loans for an employee.
/// </summary>
public sealed record GetEmployeeLoansQuery(int CompanyId, int EmployeeId) : IRequest<Result<IReadOnlyList<LoanDto>>>;

/// <summary>
/// DTO representing an individual employee loan.
/// </summary>
public sealed class LoanDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LoanDescription { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal TotalAmountToRepay { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal DeductionAmountPerPeriod { get; set; }
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<LoanRepaymentDto> Repayments { get; set; } = Array.Empty<LoanRepaymentDto>();
}

/// <summary>
/// DTO representing a single loan repayment transaction.
/// </summary>
public sealed class LoanRepaymentDto
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int PayrollRunId { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingBalanceAfter { get; set; }
    public DateTime TransactionDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Handler for <see cref="GetEmployeeLoansQuery"/>.
/// </summary>
public sealed class GetEmployeeLoansQueryHandler : IRequestHandler<GetEmployeeLoansQuery, Result<IReadOnlyList<LoanDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes dependencies.</summary>
    public GetEmployeeLoansQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LoanDto>>> Handle(GetEmployeeLoansQuery request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LoansView))
        {
            return Result<IReadOnlyList<LoanDto>>.Failure("Forbidden: You do not have permission to view loans.");
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<IReadOnlyList<LoanDto>>.Failure("Forbidden: You do not have access to this company.");
        }

        // 3. Load employee to make sure they exist and belong to the company
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null || employee.CompanyId != request.CompanyId)
        {
            return Result<IReadOnlyList<LoanDto>>.Failure("Employee not found or company mismatch.");
        }

        // 4. Retrieve loans
        var loans = await _unitOfWork.Loans.GetLoansByEmployeeAsync(request.EmployeeId, cancellationToken);

        // 5. Map to DTOs
        var dtos = loans.Select(l => new LoanDto
        {
            Id = l.Id,
            CompanyId = l.CompanyId,
            EmployeeId = l.EmployeeId,
            EmployeeName = employee.FullName,
            LoanDescription = l.LoanDescription,
            PrincipalAmount = l.PrincipalAmount,
            InterestRate = l.InterestRate,
            TotalAmountToRepay = l.TotalAmountToRepay,
            RemainingBalance = l.RemainingBalance,
            DeductionAmountPerPeriod = l.DeductionAmountPerPeriod,
            StartDate = l.StartDate,
            Status = l.Status.ToString(),
            IsActive = l.IsActive,
            Repayments = l.Repayments.Select(r => new LoanRepaymentDto
            {
                Id = r.Id,
                LoanId = r.LoanId,
                PayrollRunId = r.PayrollRunId,
                Amount = r.Amount,
                RemainingBalanceAfter = r.RemainingBalanceAfter,
                TransactionDate = r.TransactionDate,
                CreatedBy = r.CreatedBy ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList().AsReadOnly()
        }).ToList();

        return Result<IReadOnlyList<LoanDto>>.Success(dtos.AsReadOnly());
    }
}
