using FijiPayroll.Domain.Entities.Payroll;
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
/// Entity Framework Core implementation of the IPayrollRunRepository.
/// </summary>
public sealed class PayrollRunRepository : IPayrollRunRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollRunRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PayrollRun?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollRuns
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayrollRun?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns
            .IgnoreQueryFilters()
            .Include(r => r.StateHistory)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (run == null)
        {
            return null;
        }

        // Enforce ordering on base query BEFORE materialization
        var employees = await _context.PayrollRunEmployees
            .Where(e => e.PayrollRunId == id)
            .OrderBy(e => e.EmployeeId)
            .Include(e => e.LineItems)
            .Include(e => e.Trace)
            .ToListAsync(cancellationToken);

        return run;
    }

    /// <inheritdoc />
    public async Task<bool> AcquireLockAsync(int runId, Guid requestId, string userName, CancellationToken cancellationToken = default)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        bool isSqlServer = _context.Database.ProviderName?.Contains("SqlServer", System.StringComparison.OrdinalIgnoreCase) == true;
        if (isSqlServer)
        {
            transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        }

        try
        {
            var run = await _context.PayrollRuns
                .Include(r => r.StateHistory)
                .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

            if (run == null)
            {
                return false;
            }

            // Idempotency check:
            if (run.Status == PayrollRunStatus.Calculating)
            {
                if (run.CurrentRequestId == requestId)
                {
                    // Prevent duplicate concurrent execution attempts
                    return false;
                }

                // Stuck lock recovery check: Calculating state timeout is 5 minutes
                bool isStuckLock = run.LockedAt.HasValue && run.LockedAt.Value.AddMinutes(5) < DateTime.UtcNow;
                if (!isStuckLock)
                {
                    // Active calculation by another request, prevent double calculation
                    return false;
                }
            }
            else if (run.Status != PayrollRunStatus.Draft)
            {
                // Calculation can only be initiated from Draft status
                return false;
            }

            run.AcquireLock(requestId, userName);
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrency token collision
            return false;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(PayrollRun run, CancellationToken cancellationToken = default)
    {
        await _context.PayrollRuns.AddAsync(run, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<PayrollRun> Items, int TotalCount)> GetPagedAsync(
        int companyId,
        PayrollFrequencyType? frequencyFilter,
        PayrollRunStatus? statusFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollRuns
            .Where(r => r.CompanyId == companyId);

        if (frequencyFilter.HasValue)
        {
            query = query.Where(r => r.Frequency == frequencyFilter.Value);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(r => r.Status == statusFilter.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.StartDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PayrollRun>> GetByPeriodIdAsync(int periodId, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollRuns
            .Where(r => r.PayrollPeriodId == periodId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Update(PayrollRun run)
    {
        _context.PayrollRuns.Update(run);
    }
}
