using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace FijiPayroll.Persistence.Context;

/// <summary>
/// Entity Framework Core database context for the Fiji Enterprise Payroll System.
/// Implements transactional control and automatically applies auditable interceptors.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly AuditableEntityInterceptor? _auditableEntityInterceptor;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationDbContext"/> class.
    /// Used for EF migrations and design-time tooling.
    /// </summary>
    /// <param name="options">Context configuration options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationDbContext"/> class with interceptor.
    /// </summary>
    /// <param name="options">Context configuration options.</param>
    /// <param name="auditableEntityInterceptor">The interceptor for stamping audit fields.</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor)
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
    }

    /// <summary>
    /// Gets or sets the payroll components DbSet.
    /// </summary>
    public DbSet<PayrollComponent> PayrollComponents => Set<PayrollComponent>();

    /// <summary>Gets or sets the employees DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Company.Employee> Employees => Set<FijiPayroll.Domain.Entities.Company.Employee>();

    /// <summary>Gets or sets the tax brackets DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Company.TaxBracket> TaxBrackets => Set<FijiPayroll.Domain.Entities.Company.TaxBracket>();

    /// <summary>Gets or sets the payroll runs DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRun> PayrollRuns => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRun>();

    /// <summary>Gets or sets the payroll run employees DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployee> PayrollRunEmployees => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployee>();

    /// <summary>Gets or sets the payroll run employee traces DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployeeTrace> PayrollRunEmployeeTraces => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployeeTrace>();

    /// <summary>Gets or sets the payroll run line items DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunLineItem> PayrollRunLineItems => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunLineItem>();

    /// <summary>Gets or sets the payroll run state histories DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunStateHistory> PayrollRunStateHistories => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunStateHistory>();

    /// <summary>
    /// Begins a database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_currentTransaction is not null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreventTraceUpdates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreventTraceUpdates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PreventTraceUpdates()
    {
        var entries = ChangeTracker.Entries<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployeeTrace>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                throw new InvalidOperationException("TRACE_RULE_VIOLATION: Updates or modifications to PayrollRunEmployeeTrace records are strictly prohibited.");
            }
        }
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditableEntityInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
