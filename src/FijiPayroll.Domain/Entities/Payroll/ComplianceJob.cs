using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a background worker task tracked in the persistence layer.
/// Used to coordinate background thread state checks and backoff retries.
/// </summary>
public sealed class ComplianceJob : BaseEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the type/name of job being executed.</summary>
    public string JobType { get; private set; } = string.Empty;

    /// <summary>Gets the active execution status.</summary>
    public ComplianceJobStatus Status { get; private set; }

    /// <summary>Gets the error detail message if the execution failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the current attempt count.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>Gets the timestamp when this job record was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the timestamp of the last worker attempt.</summary>
    public DateTime? LastAttemptAt { get; private set; }

    /// <summary>Gets the timestamp when execution successfully finished.</summary>
    public DateTime? CompletedAt { get; private set; }

    private ComplianceJob() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new ComplianceJob.
    /// </summary>
    public static ComplianceJob Create(int companyId, string jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType)) throw new ArgumentException("Job type cannot be empty.", nameof(jobType));

        return new ComplianceJob
        {
            CompanyId = companyId,
            JobType = jobType,
            Status = ComplianceJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };
    }

    /// <summary>Updates the job status to running.</summary>
    public void StartJob()
    {
        Status = ComplianceJobStatus.Running;
        LastAttemptAt = DateTime.UtcNow;
        AttemptCount++;
    }

    /// <summary>Completes the job execution.</summary>
    public void CompleteJob()
    {
        Status = ComplianceJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Fails the job execution with a retry opportunity.</summary>
    public void FailJob(string errorMessage, bool canRetry)
    {
        ErrorMessage = errorMessage;
        Status = canRetry ? ComplianceJobStatus.Retrying : ComplianceJobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Reschedules a job for retry.</summary>
    public void ResetForRetry()
    {
        Status = ComplianceJobStatus.Pending;
        ErrorMessage = null;
    }

    /// <summary>Explicitly cancels a pending/running job.</summary>
    public void CancelJob()
    {
        Status = ComplianceJobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}
