using FijiPayroll.Domain.Entities.Payroll;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for staff Loan and LoanRepayment entities.
/// </summary>
public interface ILoanRepository
{
    /// <summary>
    /// Fetches a loan by its unique identifier, including repayments.
    /// </summary>
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all loans assigned to a specific employee.
    /// </summary>
    Task<IReadOnlyList<Loan>> GetLoansByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all loans for a given company.
    /// </summary>
    Task<IReadOnlyList<Loan>> GetLoansByCompanyAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new loan registration record.
    /// </summary>
    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new loan repayment transaction log.
    /// </summary>
    Task AddRepaymentAsync(LoanRepayment repayment, CancellationToken cancellationToken = default);
}
