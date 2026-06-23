using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Interface for aggregating dashboard KPI and alert data at the database level.
/// </summary>
public interface IDashboardRepository
{
    Task<int> GetActiveEmployeesCountAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> GetTerminatedThisMonthCountAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> GetOpenPeriodsCountAsync(int companyId, CancellationToken cancellationToken = default);
    Task<string> GetCurrentPeriodNameAsync(int companyId, CancellationToken cancellationToken = default);
    Task<string> GetCurrentRunStatusAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> GetPostedRunsCountAsync(int companyId, CancellationToken cancellationToken = default);
    Task<(decimal Gross, decimal PAYE, decimal FNPFEmployee, decimal FNPFEmployer, decimal Net)> GetLatestPostedTotalsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSystemAlertsAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(string PeriodName, decimal GrossPay, decimal PAYETax, decimal NetPay, DateTime PaymentDate)>> GetRecentRunsAsync(int companyId, int count, CancellationToken cancellationToken = default);
}
