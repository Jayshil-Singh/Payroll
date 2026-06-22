using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

public sealed class PayrollPeriodRepository : IPayrollPeriodRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollPeriodRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PayrollPeriod?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollPeriods.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollPeriod>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollPeriods
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task<PayrollPeriod?> GetOpenPeriodAsync(int companyId, PayrollFrequencyType frequency, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollPeriods
            .Where(x => x.CompanyId == companyId && x.PayrollFrequency == frequency && x.Status == PayrollPeriodStatus.Open)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(PayrollPeriod period, CancellationToken cancellationToken = default)
    {
        await _context.PayrollPeriods.AddAsync(period, cancellationToken);
    }

    public void Update(PayrollPeriod period)
    {
        _context.PayrollPeriods.Update(period);
    }
}

public sealed class PayrollGroupRepository : IPayrollGroupRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollGroupRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PayrollGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollGroups.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollGroup>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollGroups
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddAsync(PayrollGroup group, CancellationToken cancellationToken = default)
    {
        await _context.PayrollGroups.AddAsync(group, cancellationToken);
    }

    public void Update(PayrollGroup group)
    {
        _context.PayrollGroups.Update(group);
    }
}

public sealed class PayrollAdjustmentRepository : IPayrollAdjustmentRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollAdjustmentRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PayrollAdjustment>> GetUnappliedByEmployeeAsync(int companyId, int employeeId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollAdjustments
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId && !x.IsApplied && !x.IsCancelled)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddAsync(PayrollAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        await _context.PayrollAdjustments.AddAsync(adjustment, cancellationToken);
    }

    public void Update(PayrollAdjustment adjustment)
    {
        _context.PayrollAdjustments.Update(adjustment);
    }
}

public sealed class PayrollSnapshotRepository : IPayrollSnapshotRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollSnapshotRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PayrollSnapshot?> GetLatestByRunIdAsync(int runId, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollSnapshots
            .Where(x => x.PayrollRunId == runId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollSnapshot>> GetByRunIdAsync(int runId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollSnapshots
            .Where(x => x.PayrollRunId == runId)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddAsync(PayrollSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _context.PayrollSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public void Remove(PayrollSnapshot snapshot)
    {
        _context.PayrollSnapshots.Remove(snapshot);
    }
}

public sealed class PayrollExceptionQueueRepository : IPayrollExceptionQueueRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollExceptionQueueRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PayrollExceptionQueue>> GetByRunIdAsync(int runId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollExceptionQueues
            .Where(x => x.PayrollRunId == runId)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddAsync(PayrollExceptionQueue exception, CancellationToken cancellationToken = default)
    {
        await _context.PayrollExceptionQueues.AddAsync(exception, cancellationToken);
    }

    public void Remove(PayrollExceptionQueue exception)
    {
        _context.PayrollExceptionQueues.Remove(exception);
    }

    public void Update(PayrollExceptionQueue exception)
    {
        _context.PayrollExceptionQueues.Update(exception);
    }
}

public sealed class PayrollRunHistoryRepository : IPayrollRunHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollRunHistoryRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(PayrollRunHistory history, CancellationToken cancellationToken = default)
    {
        await _context.PayrollRunHistories.AddAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollRunHistory>> GetByRunIdAsync(int runId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollRunHistories
            .Where(x => x.PayrollRunId == runId)
            .ToListAsync(cancellationToken);
        return items;
    }
}

public sealed class BackgroundJobRepository : IBackgroundJobRepository
{
    private readonly ApplicationDbContext _context;

    public BackgroundJobRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<BackgroundJob?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.BackgroundJobs.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        await _context.BackgroundJobs.AddAsync(job, cancellationToken);
    }

    public void Update(BackgroundJob job)
    {
        _context.BackgroundJobs.Update(job);
    }

    public async Task<IReadOnlyList<BackgroundJob>> GetPendingJobsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "Company ID must be positive.");
        }

        var items = await _context.BackgroundJobs
            .Where(x => x.CompanyId == companyId && x.Status == "Queued")
            .ToListAsync(cancellationToken);
        return items;
    }
}

public sealed class PayrollLedgerReversalRepository : IPayrollLedgerReversalRepository
{
    private readonly ApplicationDbContext _context;

    public PayrollLedgerReversalRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(PayrollLedgerReversal reversal, CancellationToken cancellationToken = default)
    {
        await _context.PayrollLedgerReversals.AddAsync(reversal, cancellationToken);
    }
}
