using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the <see cref="IUnitOfWork"/> interface.
/// Orchestrates transaction boundaries and coordinates repository changes.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    public UnitOfWork(
        ApplicationDbContext context,
        IPayrollComponentRepository payrollComponents,
        IPayrollRunRepository payrollRuns,
        IEmployeeRepository employees,
        ITaxBracketRepository taxBrackets,
        IMasterLookupRepository masterLookups)
        : this(context, payrollComponents, payrollRuns, employees, taxBrackets, masterLookups, null!, null!, null!, null!)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="UnitOfWork"/> class with ImportJobs.
    /// </summary>
    public UnitOfWork(
        ApplicationDbContext context,
        IPayrollComponentRepository payrollComponents,
        IPayrollRunRepository payrollRuns,
        IEmployeeRepository employees,
        ITaxBracketRepository taxBrackets,
        IMasterLookupRepository masterLookups,
        IImportJobRepository importJobs)
        : this(context, payrollComponents, payrollRuns, employees, taxBrackets, masterLookups, importJobs, null!, null!, null!)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="UnitOfWork"/> class with ImportJobs and SearchIndexes.
    /// </summary>
    public UnitOfWork(
        ApplicationDbContext context,
        IPayrollComponentRepository payrollComponents,
        IPayrollRunRepository payrollRuns,
        IEmployeeRepository employees,
        ITaxBracketRepository taxBrackets,
        IMasterLookupRepository masterLookups,
        IImportJobRepository importJobs,
        ISearchIndexRepository searchIndexes)
        : this(context, payrollComponents, payrollRuns, employees, taxBrackets, masterLookups, importJobs, searchIndexes, null!, null!)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="UnitOfWork"/> class with all repositories.
    /// </summary>
    public UnitOfWork(
        ApplicationDbContext context,
        IPayrollComponentRepository payrollComponents,
        IPayrollRunRepository payrollRuns,
        IEmployeeRepository employees,
        ITaxBracketRepository taxBrackets,
        IMasterLookupRepository masterLookups,
        IImportJobRepository importJobs,
        ISearchIndexRepository searchIndexes,
        IApprovalWorkflowRepository workflows,
        ISetupRepository? setup = null,
        IComplianceRepository? compliance = null)
    {
        _context = context;
        PayrollComponents = payrollComponents;
        PayrollRuns = payrollRuns;
        Employees = employees;
        TaxBrackets = taxBrackets;
        MasterLookups = masterLookups;
        ImportJobs = importJobs ?? new ImportJobRepository(context);
        SearchIndexes = searchIndexes ?? new SearchIndexRepository(context);
        Workflows = workflows ?? new ApprovalWorkflowRepository(context);
        Setup = setup ?? new SetupRepository(context);
        Compliance = compliance ?? new ComplianceRepository(context);
        PayrollPeriods = new PayrollPeriodRepository(context);
        PayrollGroups = new PayrollGroupRepository(context);
        PayrollAdjustments = new PayrollAdjustmentRepository(context);
        PayrollSnapshots = new PayrollSnapshotRepository(context);
        PayrollExceptionQueues = new PayrollExceptionQueueRepository(context);
        PayrollRunHistories = new PayrollRunHistoryRepository(context);
        BackgroundJobs = new BackgroundJobRepository(context);
        PayrollLedgerReversals = new PayrollLedgerReversalRepository(context);
        Leave = new LeaveRepository(context);
        Loans = new LoanRepository(context);
    }

    /// <inheritdoc/>
    public ILoanRepository Loans { get; }

    /// <inheritdoc/>
    public IPayrollComponentRepository PayrollComponents { get; }

    /// <inheritdoc/>
    public IPayrollRunRepository PayrollRuns { get; }

    /// <inheritdoc/>
    public IEmployeeRepository Employees { get; }

    /// <inheritdoc/>
    public ITaxBracketRepository TaxBrackets { get; }

    /// <inheritdoc/>
    public IMasterLookupRepository MasterLookups { get; }

    /// <inheritdoc/>
    public IImportJobRepository ImportJobs { get; }

    /// <inheritdoc/>
    public ISearchIndexRepository SearchIndexes { get; }

    /// <inheritdoc/>
    public IApprovalWorkflowRepository Workflows { get; }

    /// <inheritdoc/>
    public ISetupRepository Setup { get; }

    /// <inheritdoc/>
    public IComplianceRepository Compliance { get; }

    /// <inheritdoc/>
    public IPayrollPeriodRepository PayrollPeriods { get; }

    /// <inheritdoc/>
    public IPayrollGroupRepository PayrollGroups { get; }

    /// <inheritdoc/>
    public IPayrollAdjustmentRepository PayrollAdjustments { get; }

    /// <inheritdoc/>
    public IPayrollSnapshotRepository PayrollSnapshots { get; }

    /// <inheritdoc/>
    public IPayrollExceptionQueueRepository PayrollExceptionQueues { get; }

    /// <inheritdoc/>
    public IPayrollRunHistoryRepository PayrollRunHistories { get; }

    /// <inheritdoc/>
    public IBackgroundJobRepository BackgroundJobs { get; }

    /// <inheritdoc/>
    public IPayrollLedgerReversalRepository PayrollLedgerReversals { get; }

    /// <inheritdoc/>
    public ILeaveRepository Leave { get; }

    /// <inheritdoc/>
    public async Task AddAuditLogAsync(FijiPayroll.Domain.Entities.Audit.AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.Set<FijiPayroll.Domain.Entities.Audit.AuditLog>().AddAsync(auditLog, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.CommitTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.RollbackTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the database context.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}
