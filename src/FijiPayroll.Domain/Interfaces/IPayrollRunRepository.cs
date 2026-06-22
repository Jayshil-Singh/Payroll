using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for PayrollRun aggregate.
/// </summary>
public interface IPayrollRunRepository
{
    /// <summary>
    /// Fetches run by ID.
    /// </summary>
    Task<PayrollRun?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Eagerly loads the run aggregate along with non-superseded computed employee details, trace details, and lines.
    /// </summary>
    Task<PayrollRun?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire the calculation lock on the payroll run.
    /// Validates request ID, handles timeout recovery, and prevents duplicate execution.
    /// </summary>
    Task<bool> AcquireLockAsync(int runId, Guid requestId, string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payroll run.
    /// </summary>
    Task AddAsync(PayrollRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paged run summaries.
    /// </summary>
    Task<(IReadOnlyList<PayrollRun> Items, int TotalCount)> GetPagedAsync(
        int companyId,
        PayrollFrequencyType? frequencyFilter,
        PayrollRunStatus? statusFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Fetches all payroll runs associated with a specific period.
    /// </summary>
    Task<IReadOnlyList<PayrollRun>> GetByPeriodIdAsync(int periodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payroll run.
    /// </summary>
    void Update(PayrollRun run);
}
