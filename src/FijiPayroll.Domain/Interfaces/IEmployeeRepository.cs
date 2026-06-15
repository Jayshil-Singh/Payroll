using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for Employee entity operations.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// Fetches all active employees for the company and frequency,
    /// ordered by EmployeeId (Id) ascending at the database level.
    /// </summary>
    Task<IReadOnlyList<Employee>> GetByCompanyAndFrequencyAsync(
        int companyId,
        PayrollFrequency frequency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an employee by their primary key.
    /// </summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new employee.
    /// </summary>
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
}
