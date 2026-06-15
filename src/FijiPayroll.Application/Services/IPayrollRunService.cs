using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Application service layer for payroll run orchestrations.
/// Maps presentation calls to CQRS MediatR handlers under database transactions.
/// </summary>
public interface IPayrollRunService
{
    /// <summary>
    /// Fetches detailed payroll run details, containing non-superseded employees lists.
    /// </summary>
    Task<Result<PayrollRunDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches paginated payroll runs.
    /// </summary>
    Task<Result<PagedResult<PayrollRunSummaryDto>>> GetListAsync(
        int companyId,
        PayrollFrequency? frequencyFilter = null,
        PayrollRunStatus? statusFilter = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new payroll run header.
    /// </summary>
    Task<Result<int>> CreateAsync(
        int companyId,
        string runCode,
        string periodName,
        DateTime startDate,
        DateTime endDate,
        DateTime paymentDate,
        PayrollFrequency frequency,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the run calculations under concurrency safety checks.
    /// Idempotency requestId is propagated.
    /// </summary>
    Task<Result> CalculateAsync(int id, Guid calculationRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the run calculations. Strictly non-chaining.
    /// </summary>
    Task<Result> ResetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves the run.
    /// </summary>
    Task<Result> ApproveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts the run.
    /// </summary>
    Task<Result> PostAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forced recovery of stuck locks by administrator.
    /// </summary>
    Task<Result> AdminOverrideLockAsync(int id, CancellationToken cancellationToken = default);
}
