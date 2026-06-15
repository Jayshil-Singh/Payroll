using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IEmployeeRepository.
/// </summary>
public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Employee>> GetByCompanyAndFrequencyAsync(
        int companyId,
        PayrollFrequency frequency,
        CancellationToken cancellationToken = default)
    {
        // ORDERING RULE: Must enforce ORDER BY EmployeeId at database level.
        return await _context.Employees
            .Where(e => e.CompanyId == companyId 
                     && e.Frequency == frequency 
                     && e.IsActive)
            .OrderBy(e => e.Id) // Database level ordering
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.PaymentMethods) // Include owned payment methods collection
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }
}
