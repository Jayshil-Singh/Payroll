using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Shared.Guards;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain entity representing a staff loan issued to an employee.
/// Governs principal amount, flat interest rate, period deductions, and repayment history.
/// </summary>
public sealed class Loan : SoftDeleteEntity
{
    private string _loanDescription = string.Empty;
    private readonly List<LoanRepayment> _repayments = new();

    private Loan() { }

    /// <summary>Gets the company owning this loan (isolation partition).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the employee recipient of this loan.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the loan classification descriptor (e.g., "Salary Advance").</summary>
    public string LoanDescription
    {
        get => _loanDescription;
        private set => _loanDescription = Guard.AgainstNullOrWhiteSpace(value, nameof(LoanDescription));
    }

    /// <summary>Gets the original principal loan amount.</summary>
    public decimal PrincipalAmount { get; private set; }

    /// <summary>Gets the flat interest rate percentage (e.g., 0.05 for 5% flat rate).</summary>
    public decimal InterestRate { get; private set; }

    /// <summary>Gets the total liability to repay including computed flat interest.</summary>
    public decimal TotalAmountToRepay { get; private set; }

    /// <summary>Gets the current outstanding unpaid balance of the loan.</summary>
    public decimal RemainingBalance { get; private set; }

    /// <summary>Gets the maximum deduction amount per payroll period run.</summary>
    public decimal DeductionAmountPerPeriod { get; private set; }

    /// <summary>Gets the start date of the loan amortization cycle.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the current status state of the loan.</summary>
    public LoanStatus Status { get; private set; }

    /// <summary>Gets a value indicating whether the loan is active and deductions should be run.</summary>
    public bool IsActive => Status == LoanStatus.Active;

    /// <summary>Gets the repayment history logs (read-only projection).</summary>
    public IReadOnlyCollection<LoanRepayment> Repayments => _repayments.AsReadOnly();

    /// <summary>
    /// Factory method to register a new employee staff loan.
    /// </summary>
    /// <param name="companyId">Company identifier.</param>
    /// <param name="employeeId">Employee identifier.</param>
    /// <param name="description">Staff loan descriptor details.</param>
    /// <param name="principal">Principal capital amount.</param>
    /// <param name="interestRate">Flat rate interest percentage.</param>
    /// <param name="deductionPerPeriod">Cycle deduction cap.</param>
    /// <param name="startDate">Start date of loan amortization.</param>
    /// <returns>A new initialized <see cref="Loan"/>.</returns>
    /// <exception cref="DomainException">Thrown when rules are violated.</exception>
    public static Loan Create(
        int companyId,
        int employeeId,
        string description,
        decimal principal,
        decimal interestRate,
        decimal deductionPerPeriod,
        DateTime startDate)
    {
        if (companyId <= 0)
            throw new DomainException("CompanyId must be positive.");

        if (employeeId <= 0)
            throw new DomainException("EmployeeId must be positive.");

        if (principal <= 0)
            throw new DomainException("PrincipalAmount must be greater than zero.");

        if (interestRate < 0)
            throw new DomainException("Interest rate cannot be negative.");

        if (deductionPerPeriod <= 0)
            throw new DomainException("Deduction amount per period must be greater than zero.");

        decimal totalAmount = principal * (1m + interestRate);

        return new Loan
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            LoanDescription = description,
            PrincipalAmount = principal,
            InterestRate = interestRate,
            TotalAmountToRepay = totalAmount,
            RemainingBalance = totalAmount,
            DeductionAmountPerPeriod = deductionPerPeriod,
            StartDate = startDate.Date,
            Status = LoanStatus.Active
        };
    }

    /// <summary>
    /// Records a payroll deduction repayment transaction and reduces the remaining balance.
    /// </summary>
    /// <param name="payrollRunId">Payroll run identifier.</param>
    /// <param name="amount">Amount processed.</param>
    /// <param name="recordedBy">Operator name.</param>
    /// <exception cref="DomainException">Thrown if loan is fully paid or amount is invalid.</exception>
    public void RecordRepayment(int payrollRunId, decimal amount, string recordedBy)
    {
        if (amount <= 0)
            throw new DomainException("Repayment deduction amount must be greater than zero.");

        if (Status == LoanStatus.FullyPaid)
            throw new DomainException("This loan is already fully paid.");

        decimal deduction = Math.Min(amount, RemainingBalance);
        RemainingBalance -= deduction;

        var repayment = LoanRepayment.Create(Id, payrollRunId, deduction, RemainingBalance, DateTime.UtcNow);
        repayment.CreatedBy = recordedBy;
        repayment.CreatedAt = DateTime.UtcNow;
        _repayments.Add(repayment);

        if (RemainingBalance <= 0)
        {
            Status = LoanStatus.FullyPaid;
        }
    }

    /// <summary>
    /// Suspends active deductions for this loan.
    /// </summary>
    /// <exception cref="DomainException">Thrown if not currently active.</exception>
    public void Suspend()
    {
        if (Status != LoanStatus.Active)
            throw new DomainException($"Cannot suspend loan. Current status is {Status}.");

        Status = LoanStatus.Suspended;
    }

    /// <summary>
    /// Resumes suspended deductions for this loan.
    /// </summary>
    /// <exception cref="DomainException">Thrown if not currently suspended.</exception>
    public void Resume()
    {
        if (Status != LoanStatus.Suspended)
            throw new DomainException($"Cannot resume loan. Current status is {Status}.");

        Status = LoanStatus.Active;
    }

    /// <summary>
    /// Writes off any outstanding remaining balance on the loan.
    /// </summary>
    public void WriteOff()
    {
        if (Status == LoanStatus.FullyPaid)
            throw new DomainException("Cannot write off a fully paid loan.");

        RemainingBalance = 0m;
        Status = LoanStatus.WrittenOff;
    }

    /// <summary>
    /// Reverses/removes any repayments recorded for a specific payroll run, restoring the remaining balance.
    /// </summary>
    public void ReverseRepayment(int payrollRunId)
    {
        var repaymentsToRemove = _repayments.Where(r => r.PayrollRunId == payrollRunId).ToList();
        foreach (var repayment in repaymentsToRemove)
        {
            RemainingBalance += repayment.Amount;
            _repayments.Remove(repayment);
        }

        if (RemainingBalance > 0 && (Status == LoanStatus.FullyPaid || Status == LoanStatus.WrittenOff))
        {
            Status = LoanStatus.Active;
        }
    }
}
