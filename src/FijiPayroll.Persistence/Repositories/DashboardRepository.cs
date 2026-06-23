using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> GetActiveEmployeesCountAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .CountAsync(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted, cancellationToken);
    }

    public async Task<int> GetTerminatedThisMonthCountAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _context.Employees
            .CountAsync(x => x.CompanyId == companyId && !x.IsActive && x.TerminationDate >= startOfMonth, cancellationToken);
    }

    public async Task<int> GetOpenPeriodsCountAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollPeriods
            .CountAsync(x => x.CompanyId == companyId && x.Status == PayrollPeriodStatus.Open, cancellationToken);
    }

    public async Task<string> GetCurrentPeriodNameAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Where(x => x.CompanyId == companyId && x.Status == PayrollPeriodStatus.Open)
            .OrderByDescending(x => x.StartDate)
            .Select(x => x.PeriodCode)
            .FirstOrDefaultAsync(cancellationToken);

        return period ?? "N/A";
    }

    public async Task<string> GetCurrentRunStatusAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var runStatus = await _context.PayrollRuns
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return runStatus != default ? runStatus.ToString() : "No Active Run";
    }

    public async Task<int> GetPostedRunsCountAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollRuns
            .CountAsync(x => x.CompanyId == companyId && 
                             (x.Status == PayrollRunStatus.Posted || 
                              x.Status == PayrollRunStatus.Locked || 
                              x.Status == PayrollRunStatus.Archived), cancellationToken);
    }

    public async Task<(decimal Gross, decimal PAYE, decimal FNPFEmployee, decimal FNPFEmployer, decimal Net)> GetLatestPostedTotalsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var ledger = await _context.PayrollLedgers
            .Where(x => x.CompanyId == companyId && !x.IsReversed)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (ledger == null)
        {
            return (0, 0, 0, 0, 0);
        }

        return (ledger.TotalGross, ledger.TotalPAYE, ledger.TotalFNPFEmployee, ledger.TotalFNPFEmployer, ledger.TotalNetPay);
    }

    public async Task<IReadOnlyList<string>> GetSystemAlertsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var alerts = new List<string>();

        // 1. Pending approval runs
        int pendingApprovals = await _context.PayrollRuns
            .CountAsync(x => x.CompanyId == companyId && x.Status == PayrollRunStatus.Calculated, cancellationToken);
        if (pendingApprovals > 0)
        {
            alerts.Add($"Pending Approval: {pendingApprovals} payroll run(s) awaiting validation.");
        }

        // 2. Failed compliance jobs
        int failedJobs = await _context.ComplianceJobs
            .CountAsync(x => x.CompanyId == companyId && x.Status == ComplianceJobStatus.Failed, cancellationToken);
        if (failedJobs > 0)
        {
            alerts.Add($"Compliance Issue: {failedJobs} file processing job(s) failed.");
        }

        // 3. Pending notifications
        int pendingNotifications = await _context.Notifications
            .CountAsync(x => x.CompanyId == companyId && x.Status == "Pending", cancellationToken);
        if (pendingNotifications > 0)
        {
            alerts.Add($"System Queue: {pendingNotifications} notifications waiting in outbound pool.");
        }

        // 4. Missing details employees
        int missingDetails = await _context.Employees
            .CountAsync(x => x.CompanyId == companyId && (string.IsNullOrEmpty(x.Tin) || string.IsNullOrEmpty(x.FnpfNumber)), cancellationToken);
        if (missingDetails > 0)
        {
            alerts.Add($"Data Quality: {missingDetails} employee record(s) lack required TIN or FNPF identifiers.");
        }

        return alerts;
    }

    public async Task<IReadOnlyList<(string PeriodName, decimal GrossPay, decimal PAYETax, decimal NetPay, DateTime PaymentDate)>> GetRecentRunsAsync(int companyId, int count, CancellationToken cancellationToken = default)
    {
        var query = from l in _context.PayrollLedgers
                    join r in _context.PayrollRuns on l.PayrollRunId equals r.Id
                    join p in _context.PayrollPeriods on r.PayrollPeriodId equals (int?)p.Id
                    where l.CompanyId == companyId && !l.IsReversed
                    orderby l.CreatedUtc descending
                    select new
                    {
                        PeriodName = p.PeriodCode,
                        GrossPay = l.TotalGross,
                        PAYETax = l.TotalPAYE,
                        NetPay = l.TotalNetPay,
                        PaymentDate = p.PaymentDate
                    };

        var list = await query.Take(count).ToListAsync(cancellationToken);
        return list.Select(x => (x.PeriodName, x.GrossPay, x.PAYETax, x.NetPay, x.PaymentDate)).ToList();
    }
}
