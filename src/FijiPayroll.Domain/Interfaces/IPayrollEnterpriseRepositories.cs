using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>Repository for Payroll Period lifecycle control.</summary>
public interface IPayrollPeriodRepository
{
    /// <summary>Gets a period by ID.</summary>
    Task<PayrollPeriod?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets all periods for a company.</summary>
    Task<IReadOnlyList<PayrollPeriod>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Gets the open period for a company and frequency.</summary>
    Task<PayrollPeriod?> GetOpenPeriodAsync(int companyId, PayrollFrequencyType frequency, CancellationToken cancellationToken = default);

    /// <summary>Adds a new period.</summary>
    Task AddAsync(PayrollPeriod period, CancellationToken cancellationToken = default);

    /// <summary>Updates a period.</summary>
    void Update(PayrollPeriod period);
}

/// <summary>Repository for Payroll Groups.</summary>
public interface IPayrollGroupRepository
{
    /// <summary>Gets a group by ID.</summary>
    Task<PayrollGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets all groups for a company.</summary>
    Task<IReadOnlyList<PayrollGroup>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new group.</summary>
    Task AddAsync(PayrollGroup group, CancellationToken cancellationToken = default);

    /// <summary>Updates a group.</summary>
    void Update(PayrollGroup group);
}

/// <summary>Repository for manual adjustments.</summary>
public interface IPayrollAdjustmentRepository
{
    /// <summary>Gets unapplied adjustments for an employee.</summary>
    Task<IReadOnlyList<PayrollAdjustment>> GetUnappliedByEmployeeAsync(int companyId, int employeeId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new adjustment.</summary>
    Task AddAsync(PayrollAdjustment adjustment, CancellationToken cancellationToken = default);

    /// <summary>Updates an adjustment.</summary>
    void Update(PayrollAdjustment adjustment);
}

/// <summary>Repository for compressed calculation snapshots.</summary>
public interface IPayrollSnapshotRepository
{
    /// <summary>Gets the latest snapshot for a run.</summary>
    Task<PayrollSnapshot?> GetLatestByRunIdAsync(int runId, CancellationToken cancellationToken = default);

    /// <summary>Gets all snapshots for a run.</summary>
    Task<IReadOnlyList<PayrollSnapshot>> GetByRunIdAsync(int runId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new snapshot.</summary>
    Task AddAsync(PayrollSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Removes a snapshot.</summary>
    void Remove(PayrollSnapshot snapshot);
}

/// <summary>Repository for exception queues.</summary>
public interface IPayrollExceptionQueueRepository
{
    /// <summary>Gets all exceptions for a run.</summary>
    Task<IReadOnlyList<PayrollExceptionQueue>> GetByRunIdAsync(int runId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new exception.</summary>
    Task AddAsync(PayrollExceptionQueue exception, CancellationToken cancellationToken = default);

    /// <summary>Removes an exception.</summary>
    void Remove(PayrollExceptionQueue exception);

    /// <summary>Updates an exception.</summary>
    void Update(PayrollExceptionQueue exception);
}

/// <summary>Repository for immutable run history audits.</summary>
public interface IPayrollRunHistoryRepository
{
    /// <summary>Adds a history entry.</summary>
    Task AddAsync(PayrollRunHistory history, CancellationToken cancellationToken = default);

    /// <summary>Gets all history for a run.</summary>
    Task<IReadOnlyList<PayrollRunHistory>> GetByRunIdAsync(int runId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for scheduled background execution jobs.</summary>
public interface IBackgroundJobRepository
{
    /// <summary>Gets a job by ID.</summary>
    Task<BackgroundJob?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new job.</summary>
    Task AddAsync(BackgroundJob job, CancellationToken cancellationToken = default);

    /// <summary>Updates a job.</summary>
    void Update(BackgroundJob job);

    /// <summary>Gets pending jobs for a specific company tenant.</summary>
    Task<IReadOnlyList<BackgroundJob>> GetPendingJobsAsync(int companyId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for double-entry reversals.</summary>
public interface IPayrollLedgerReversalRepository
{
    /// <summary>Adds a ledger reversal.</summary>
    Task AddAsync(PayrollLedgerReversal reversal, CancellationToken cancellationToken = default);
}
