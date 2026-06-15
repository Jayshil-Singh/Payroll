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
    /// <param name="context">The database context.</param>
    /// <param name="payrollComponents">The payroll component repository.</param>
    public UnitOfWork(
        ApplicationDbContext context,
        IPayrollComponentRepository payrollComponents,
        IPayrollRunRepository payrollRuns,
        IEmployeeRepository employees,
        ITaxBracketRepository taxBrackets)
    {
        _context = context;
        PayrollComponents = payrollComponents;
        PayrollRuns = payrollRuns;
        Employees = employees;
        TaxBrackets = taxBrackets;
    }

    /// <inheritdoc/>
    public IPayrollComponentRepository PayrollComponents { get; }

    /// <inheritdoc/>
    public IPayrollRunRepository PayrollRuns { get; }

    /// <inheritdoc/>
    public IEmployeeRepository Employees { get; }

    /// <inheritdoc/>
    public ITaxBracketRepository TaxBrackets { get; }

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
