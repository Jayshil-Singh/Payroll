namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Unit of Work interface providing a single save-point for all repository operations.
/// Ensures atomicity — either all changes within a business operation are committed,
/// or none are. Implementations wrap EF Core's DbContext.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Repository for payroll component operations.</summary>
    IPayrollComponentRepository PayrollComponents { get; }

    /// <summary>Repository for payroll runs aggregate operations.</summary>
    IPayrollRunRepository PayrollRuns { get; }

    /// <summary>Repository for employee master operations.</summary>
    IEmployeeRepository Employees { get; }

    /// <summary>Repository for tax rules operations.</summary>
    ITaxBracketRepository TaxBrackets { get; }

    /// <summary>Repository for master lookup operations.</summary>
    IMasterLookupRepository MasterLookups { get; }

    /// <summary>Repository for import job operations.</summary>
    IImportJobRepository ImportJobs { get; }

    /// <summary>Repository for search index operations.</summary>
    ISearchIndexRepository SearchIndexes { get; }

    /// <summary>
    /// Persists all pending changes tracked by EF Core to the database in a single
    /// atomic operation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit database transaction for operations that span multiple
    /// save points. Use only when a single <see cref="SaveChangesAsync"/> is insufficient.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the active explicit transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the active explicit transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
