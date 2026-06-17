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
        PayrollFrequencyType frequency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an employee by their primary key.
    /// </summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new employee.
    /// </summary>
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a server-side filtered and paginated list of employees.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="searchTerm">Optional search term to filter by name.</param>
    /// <param name="departmentFilter">Optional department to filter by.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the read-only list of employees and the total count.</returns>
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int companyId,
        string? searchTerm,
        string? departmentFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of employees matching the specified IDs.
    /// </summary>
    Task<IReadOnlyList<Employee>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
