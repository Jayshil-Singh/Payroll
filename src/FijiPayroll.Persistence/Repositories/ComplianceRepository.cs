using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IComplianceRepository.
/// </summary>
public sealed class ComplianceRepository : IComplianceRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceRepository"/> class.
    /// </summary>
    public ComplianceRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ── CompliancePeriod ──────────────────────────────────────────────────
    public async Task<CompliancePeriod?> GetPeriodByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CompliancePeriods.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<CompliancePeriod?> GetActivePeriodAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.CompliancePeriods
            .Where(x => x.CompanyId == companyId && x.Status == CompliancePeriodStatus.Open)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddPeriodAsync(CompliancePeriod period, CancellationToken cancellationToken = default)
    {
        await _context.CompliancePeriods.AddAsync(period, cancellationToken);
    }

    // ── ComplianceBatch ───────────────────────────────────────────────────
    public async Task<ComplianceBatch?> GetBatchByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceBatches.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddBatchAsync(ComplianceBatch batch, CancellationToken cancellationToken = default)
    {
        await _context.ComplianceBatches.AddAsync(batch, cancellationToken);
    }

    // ── PayrollLedger ─────────────────────────────────────────────────────
    public async Task<IReadOnlyList<PayrollLedger>> GetLedgerByRunIdAsync(int payrollRunId, CancellationToken cancellationToken = default)
    {
        var items = await _context.PayrollLedgers
            .Where(x => x.PayrollRunId == payrollRunId)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddLedgerAsync(PayrollLedger ledger, CancellationToken cancellationToken = default)
    {
        await _context.PayrollLedgers.AddAsync(ledger, cancellationToken);
    }

    // ── ComplianceEvent ───────────────────────────────────────────────────
    public async Task AddComplianceEventAsync(ComplianceEvent ev, CancellationToken cancellationToken = default)
    {
        await _context.ComplianceEvents.AddAsync(ev, cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceEvent>> GetEventsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ComplianceEvents
            .Where(x => x.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);
        return items;
    }

    // ── ApprovalMatrix ────────────────────────────────────────────────────
    public async Task<ApprovalMatrix?> GetApprovalMatrixAsync(int companyId, ApprovalRole role, string actionType, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalMatrices
            .Where(x => x.CompanyId == companyId && x.Role == role && x.ActionType == actionType && x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddApprovalMatrixAsync(ApprovalMatrix matrix, CancellationToken cancellationToken = default)
    {
        await _context.ApprovalMatrices.AddAsync(matrix, cancellationToken);
    }

    // ── ComplianceSnapshot ────────────────────────────────────────────────
    public async Task<ComplianceSnapshot?> GetSnapshotByBatchIdAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceSnapshots
            .Where(x => x.ComplianceBatchId == batchId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddSnapshotAsync(ComplianceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _context.ComplianceSnapshots.AddAsync(snapshot, cancellationToken);
    }

    // ── ComplianceAmendment ───────────────────────────────────────────────
    public async Task<ComplianceAmendment?> GetAmendmentByCurrentIdAsync(int currentId, CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceAmendments
            .Where(x => x.CurrentSubmissionId == currentId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAmendmentAsync(ComplianceAmendment amendment, CancellationToken cancellationToken = default)
    {
        await _context.ComplianceAmendments.AddAsync(amendment, cancellationToken);
    }

    // ── StatutoryRule ─────────────────────────────────────────────────────
    public async Task<StatutoryRule?> GetStatutoryRuleAsync(string authority, string ruleCode, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.StatutoryRules
            .Where(x => x.Authority == authority && x.RuleCode == ruleCode && x.IsActive)
            .Where(x => x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddStatutoryRuleAsync(StatutoryRule rule, CancellationToken cancellationToken = default)
    {
        await _context.StatutoryRules.AddAsync(rule, cancellationToken);
    }

    // ── FileLayoutDefinition ──────────────────────────────────────────────
    public async Task<FileLayoutDefinition?> GetFileLayoutAsync(string ownerCode, string layoutType, CancellationToken cancellationToken = default)
    {
        return await _context.FileLayoutDefinitions
            .Where(x => x.OwnerCode == ownerCode && x.LayoutType == layoutType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddFileLayoutAsync(FileLayoutDefinition layout, CancellationToken cancellationToken = default)
    {
        await _context.FileLayoutDefinitions.AddAsync(layout, cancellationToken);
    }

    // ── ComplianceJob ─────────────────────────────────────────────────────
    public async Task<ComplianceJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceJobs.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddJobAsync(ComplianceJob job, CancellationToken cancellationToken = default)
    {
        await _context.ComplianceJobs.AddAsync(job, cancellationToken);
    }

    // ── Notification ──────────────────────────────────────────────────────
    public async Task<Notification?> GetNotificationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetPendingNotificationsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var items = await _context.Notifications
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return items;
    }

    // ── FRCSSubmission ────────────────────────────────────────────────────
    public async Task<FRCSSubmission?> GetFRCSSubmissionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FRCSSubmissions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddFRCSSubmissionAsync(FRCSSubmission submission, CancellationToken cancellationToken = default)
    {
        await _context.FRCSSubmissions.AddAsync(submission, cancellationToken);
    }

    // ── FNPFSubmission ────────────────────────────────────────────────────
    public async Task<FNPFSubmission?> GetFNPFSubmissionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FNPFSubmissions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddFNPFSubmissionAsync(FNPFSubmission submission, CancellationToken cancellationToken = default)
    {
        await _context.FNPFSubmissions.AddAsync(submission, cancellationToken);
    }

    // ── BankFile ──────────────────────────────────────────────────────────
    public async Task<BankFile?> GetBankFileByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.BankFiles.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddBankFileAsync(BankFile bankFile, CancellationToken cancellationToken = default)
    {
        await _context.BankFiles.AddAsync(bankFile, cancellationToken);
    }

    // ── Queries ───────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<FRCSSubmission>> GetRecentFRCSSubmissionsAsync(int companyId, int count, CancellationToken cancellationToken = default)
    {
        var items = await _context.FRCSSubmissions
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<FNPFSubmission>> GetRecentFNPFSubmissionsAsync(int companyId, int count, CancellationToken cancellationToken = default)
    {
        var items = await _context.FNPFSubmissions
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<BankFile>> GetRecentBankFilesAsync(int companyId, int count, CancellationToken cancellationToken = default)
    {
        var items = await _context.BankFiles
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task<int> GetEmployeeMissingDetailsCountAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(x.Tin) || string.IsNullOrEmpty(x.FnpfNumber)))
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveJobsCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceJobs
            .CountAsync(x => x.Status == ComplianceJobStatus.Pending || x.Status == ComplianceJobStatus.Running, cancellationToken);
    }

    public async Task<int> GetPendingNotificationsCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(x => x.Status == "Pending", cancellationToken);
    }

    public async Task<FRCSSubmission?> GetLatestFRCSSubmissionAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.FRCSSubmissions
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FNPFSubmission?> GetLatestFNPFSubmissionAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.FNPFSubmissions
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ComplianceJob?> GetNextJobToProcessAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceJobs
            .Where(x => x.Status == ComplianceJobStatus.Pending || x.Status == ComplianceJobStatus.Retrying)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
