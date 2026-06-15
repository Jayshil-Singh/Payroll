using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for <see cref="PayrollComponent"/> aggregate.
/// Provides data access methods specific to the payroll component domain.
/// Implementations live in the Persistence layer.
/// </summary>
public interface IPayrollComponentRepository
{
    /// <summary>
    /// Retrieves a payroll component by its primary key.
    /// Returns <c>null</c> if not found or soft-deleted.
    /// </summary>
    /// <param name="id">The component primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PayrollComponent?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active (not soft-deleted) payroll components for a given company,
    /// ordered by <see cref="PayrollComponent.DisplayOrder"/> ascending.
    /// </summary>
    /// <param name="companyId">The company to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PayrollComponent>> GetByCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active payroll components for a company filtered by type.
    /// </summary>
    /// <param name="companyId">The company to query.</param>
    /// <param name="componentType">The type filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PayrollComponent>> GetByCompanyAndTypeAsync(
        int companyId,
        ComponentType componentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if a component with <paramref name="componentCode"/>
    /// already exists for the company (case-insensitive, excluding soft-deleted records).
    /// </summary>
    /// <param name="companyId">The company to check within.</param>
    /// <param name="componentCode">The code to check.</param>
    /// <param name="excludeId">
    /// Optional existing component ID to exclude from the check (used during updates).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> CodeExistsAsync(
        int companyId,
        string componentCode,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if any payroll run detail references this component,
    /// indicating it cannot be safely deleted.
    /// </summary>
    /// <param name="componentId">The component to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsUsedInPayrollRunsAsync(
        int componentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new <see cref="PayrollComponent"/> to the persistence store.
    /// Caller must call <see cref="IUnitOfWork.SaveChangesAsync"/> to commit.
    /// </summary>
    Task AddAsync(PayrollComponent component, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a <see cref="PayrollComponent"/> as modified in the change tracker.
    /// Caller must call <see cref="IUnitOfWork.SaveChangesAsync"/> to commit.
    /// </summary>
    void Update(PayrollComponent component);

    /// <summary>
    /// Returns the maximum current DisplayOrder value for a company,
    /// useful for appending a new component at the end.
    /// </summary>
    Task<int> GetMaxDisplayOrderAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a server-side filtered and paginated list of payroll components.
    /// </summary>
    Task<(IReadOnlyList<PayrollComponent> Items, int TotalCount)> GetPagedAsync(
        int companyId,
        string? searchTerm,
        ComponentType? typeFilter,
        bool activeOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
