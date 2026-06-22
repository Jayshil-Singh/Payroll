using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework implementation of <see cref="ILoanRepository"/>.
/// </summary>
public sealed class LoanRepository : ILoanRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes dependencies.</summary>
    public LoanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.StaffLoans
            .Include(x => x.Repayments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Loan>> GetLoansByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffLoans
            .Include(x => x.Repayments)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Loan>> GetLoansByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffLoans
            .Include(x => x.Repayments)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _context.StaffLoans.AddAsync(loan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRepaymentAsync(LoanRepayment repayment, CancellationToken cancellationToken = default)
    {
        await _context.StaffLoanRepayments.AddAsync(repayment, cancellationToken);
    }
}
