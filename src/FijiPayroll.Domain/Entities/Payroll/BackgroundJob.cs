using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model for scheduling, monitoring, and executing background tasks.
/// </summary>
public sealed class BackgroundJob : BaseEntity
{
    public int CompanyId { get; private set; }
    public string JobType { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Queued"; // Queued, Running, Completed, Failed, Cancelled
    public string? Parameters { get; private set; }
    public int Progress { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime ScheduledUtc { get; private set; }
    public DateTime? StartedUtc { get; private set; }
    public DateTime? CompletedUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }

    private BackgroundJob() { } // For EF Core

    public static BackgroundJob Create(
        int companyId,
        string jobType,
        string? parameters,
        DateTime scheduledUtc,
        string createdBy)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (string.IsNullOrWhiteSpace(jobType)) throw new ArgumentException("Job type is required.", nameof(jobType));

        return new BackgroundJob
        {
            CompanyId = companyId,
            JobType = jobType,
            Parameters = parameters,
            Status = "Queued",
            Progress = 0,
            CreatedUtc = DateTime.UtcNow,
            ScheduledUtc = scheduledUtc,
            CreatedBy = createdBy,
            RetryCount = 0
        };
    }

    public void Start()
    {
        Status = "Running";
        StartedUtc = DateTime.UtcNow;
    }

    public void UpdateProgress(int progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
    }

    public void Complete()
    {
        Status = "Completed";
        Progress = 100;
        CompletedUtc = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        CompletedUtc = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        Status = "Queued";
    }

    public void Cancel()
    {
        Status = "Cancelled";
        CompletedUtc = DateTime.UtcNow;
    }
}
