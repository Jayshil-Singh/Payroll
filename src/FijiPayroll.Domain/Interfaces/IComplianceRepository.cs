using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for all Compliance-related domain entities.
/// </summary>
public interface IComplianceRepository
{
    // CompliancePeriod
    Task<CompliancePeriod?> GetPeriodByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CompliancePeriod?> GetActivePeriodAsync(int companyId, CancellationToken cancellationToken = default);
    Task AddPeriodAsync(CompliancePeriod period, CancellationToken cancellationToken = default);

    // ComplianceBatch
    Task<ComplianceBatch?> GetBatchByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddBatchAsync(ComplianceBatch batch, CancellationToken cancellationToken = default);

    // PayrollLedger
    Task<IReadOnlyList<PayrollLedgerEmployee>> GetLedgerByRunIdAsync(int payrollRunId, CancellationToken cancellationToken = default);
    Task<PayrollLedger?> GetLedgerHeaderByRunIdAsync(int payrollRunId, CancellationToken cancellationToken = default);
    Task AddLedgerAsync(PayrollLedger ledger, CancellationToken cancellationToken = default);

    // ComplianceEvent
    Task AddComplianceEventAsync(ComplianceEvent ev, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceEvent>> GetEventsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    // ApprovalMatrix
    Task<ApprovalMatrix?> GetApprovalMatrixAsync(int companyId, ApprovalRole role, string actionType, CancellationToken cancellationToken = default);
    Task AddApprovalMatrixAsync(ApprovalMatrix matrix, CancellationToken cancellationToken = default);

    // ComplianceSnapshot
    Task<ComplianceSnapshot?> GetSnapshotByBatchIdAsync(int batchId, CancellationToken cancellationToken = default);
    Task AddSnapshotAsync(ComplianceSnapshot snapshot, CancellationToken cancellationToken = default);

    // ComplianceAmendment
    Task<ComplianceAmendment?> GetAmendmentByCurrentIdAsync(int currentId, CancellationToken cancellationToken = default);
    Task AddAmendmentAsync(ComplianceAmendment amendment, CancellationToken cancellationToken = default);

    // StatutoryRule
    Task<StatutoryRule?> GetStatutoryRuleAsync(string authority, string ruleCode, DateTime date, CancellationToken cancellationToken = default);
    Task AddStatutoryRuleAsync(StatutoryRule rule, CancellationToken cancellationToken = default);

    // FileLayoutDefinition
    Task<FileLayoutDefinition?> GetFileLayoutAsync(string ownerCode, string layoutType, CancellationToken cancellationToken = default);
    Task AddFileLayoutAsync(FileLayoutDefinition layout, CancellationToken cancellationToken = default);

    // ComplianceJob
    Task<ComplianceJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddJobAsync(ComplianceJob job, CancellationToken cancellationToken = default);

    // Notification
    Task<Notification?> GetNotificationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetPendingNotificationsAsync(int limit, CancellationToken cancellationToken = default);

    // FRCSSubmission
    Task<FRCSSubmission?> GetFRCSSubmissionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddFRCSSubmissionAsync(FRCSSubmission submission, CancellationToken cancellationToken = default);

    // FNPFSubmission
    Task<FNPFSubmission?> GetFNPFSubmissionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddFNPFSubmissionAsync(FNPFSubmission submission, CancellationToken cancellationToken = default);

    // BankFile
    Task<BankFile?> GetBankFileByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddBankFileAsync(BankFile bankFile, CancellationToken cancellationToken = default);

    // Queries
    Task<IReadOnlyList<FRCSSubmission>> GetRecentFRCSSubmissionsAsync(int companyId, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FNPFSubmission>> GetRecentFNPFSubmissionsAsync(int companyId, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankFile>> GetRecentBankFilesAsync(int companyId, int count, CancellationToken cancellationToken = default);
    Task<int> GetEmployeeMissingDetailsCountAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> GetActiveJobsCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingNotificationsCountAsync(CancellationToken cancellationToken = default);
    Task<FRCSSubmission?> GetLatestFRCSSubmissionAsync(int companyId, CancellationToken cancellationToken = default);
    Task<FNPFSubmission?> GetLatestFNPFSubmissionAsync(int companyId, CancellationToken cancellationToken = default);
    Task<ComplianceJob?> GetNextJobToProcessAsync(CancellationToken cancellationToken = default);
}
