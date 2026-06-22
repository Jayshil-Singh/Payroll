using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Exceptions;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain entity representing an individual staff loan repayment transaction.
/// Records historical payment transactions linked to a payroll run.
/// </summary>
public sealed class LoanRepayment : AuditableEntity
{
    private LoanRepayment() { }

    /// <summary>Gets the parent loan identifier.</summary>
    public int LoanId { get; private set; }

    /// <summary>Gets the payroll run identifier in which this repayment occurred.</summary>
    public int PayrollRunId { get; private set; }

    /// <summary>Gets the amount of the repayment deduction.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the loan remaining balance immediately after this repayment transaction.</summary>
    public decimal RemainingBalanceAfter { get; private set; }

    /// <summary>Gets the transaction execution date.</summary>
    public DateTime TransactionDate { get; private set; }

    /// <summary>
    /// Factory method to create a new staff loan repayment entry.
    /// </summary>
    /// <param name="loanId">Loan identifier.</param>
    /// <param name="payrollRunId">Payroll run identifier.</param>
    /// <param name="amount">Amount deducted.</param>
    /// <param name="remainingBalanceAfter">Remaining balance after payment.</param>
    /// <param name="transactionDate">Execution timestamp.</param>
    /// <returns>A new <see cref="LoanRepayment"/>.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static LoanRepayment Create(
        int loanId,
        int payrollRunId,
        decimal amount,
        decimal remainingBalanceAfter,
        DateTime transactionDate)
    {
        if (loanId <= 0)
            throw new DomainException("LoanId must be positive.");

        if (payrollRunId <= 0)
            throw new DomainException("PayrollRunId must be positive.");

        if (amount <= 0)
            throw new DomainException("Repayment amount must be greater than zero.");

        if (remainingBalanceAfter < 0)
            throw new DomainException("Remaining balance after repayment cannot be negative.");

        return new LoanRepayment
        {
            LoanId = loanId,
            PayrollRunId = payrollRunId,
            Amount = amount,
            RemainingBalanceAfter = remainingBalanceAfter,
            TransactionDate = transactionDate
        };
    }
}
