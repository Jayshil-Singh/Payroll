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

    /// <summary>Repository for approval workflow operations.</summary>
    IApprovalWorkflowRepository Workflows { get; }

    /// <summary>Repository for Company Setup Wizard operations.</summary>
    ISetupRepository Setup { get; }

    /// <summary>Repository for Compliance aggregate operations.</summary>
    IComplianceRepository Compliance { get; }

    /// <summary>Repository for payroll periods.</summary>
    IPayrollPeriodRepository PayrollPeriods { get; }

    /// <summary>Repository for payroll groups.</summary>
    IPayrollGroupRepository PayrollGroups { get; }

    /// <summary>Repository for payroll adjustments.</summary>
    IPayrollAdjustmentRepository PayrollAdjustments { get; }

    /// <summary>Repository for payroll snapshots.</summary>
    IPayrollSnapshotRepository PayrollSnapshots { get; }

    /// <summary>Repository for payroll exception queues.</summary>
    IPayrollExceptionQueueRepository PayrollExceptionQueues { get; }

    /// <summary>Repository for payroll run histories.</summary>
    IPayrollRunHistoryRepository PayrollRunHistories { get; }

    /// <summary>Repository for background jobs.</summary>
    IBackgroundJobRepository BackgroundJobs { get; }

    /// <summary>Repository for Leave management operations.</summary>
    ILeaveRepository Leave { get; }

    /// <summary>Repository for staff Loan and LoanRepayment operations.</summary>
    ILoanRepository Loans { get; }

    /// <summary>Repository for payroll ledger reversals.</summary>
    IPayrollLedgerReversalRepository PayrollLedgerReversals { get; }

    /// <summary>
    /// Adds an audit log entry to the database context.
    /// </summary>
    Task AddAuditLogAsync(FijiPayroll.Domain.Entities.Audit.AuditLog auditLog, CancellationToken cancellationToken = default);

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
