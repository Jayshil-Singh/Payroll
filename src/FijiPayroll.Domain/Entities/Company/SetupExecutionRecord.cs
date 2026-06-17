using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a transaction execution run log for setup onboarding steps.
/// Guarantees idempotency via database unique index constraints on (CompanyId, ExecutionId).
/// </summary>
public sealed class SetupExecutionRecord : SoftDeleteEntity
{
    private SetupExecutionRecord() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the unique execution request identifier.</summary>
    public Guid ExecutionId { get; private set; }

    /// <summary>Gets the timestamp when execution started.</summary>
    public DateTime StartedUtc { get; private set; }

    /// <summary>Gets the timestamp when execution completed.</summary>
    public DateTime? CompletedUtc { get; private set; }

    /// <summary>Gets the execution duration in milliseconds.</summary>
    public long? DurationMilliseconds { get; private set; }

    /// <summary>Gets the host machine name executing the wizard.</summary>
    public string MachineName { get; private set; } = string.Empty;

    /// <summary>Gets the executing application version.</summary>
    public string ApplicationVersion { get; private set; } = string.Empty;

    /// <summary>Gets the exception error message if execution failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the exception error stack trace details.</summary>
    public string? ErrorStackTrace { get; private set; }

    /// <summary>Gets the current status state of this execution run.</summary>
    public ExecutionStatus Status { get; private set; } = ExecutionStatus.Pending;

    /// <summary>Factory method to build a new SetupExecutionRecord.</summary>
    public static SetupExecutionRecord Create(
        int companyId,
        Guid executionId,
        string machineName,
        string appVersion)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (executionId == Guid.Empty)
            throw new ArgumentException("Execution ID Guid cannot be empty.", nameof(executionId));

        return new SetupExecutionRecord
        {
            CompanyId = companyId,
            ExecutionId = executionId,
            StartedUtc = DateTime.UtcNow,
            MachineName = machineName ?? Environment.MachineName,
            ApplicationVersion = appVersion ?? "1.0.0",
            Status = ExecutionStatus.Running
        };
    }

    /// <summary>Marks this execution run completed successfully.</summary>
    public void MarkCompleted()
    {
        Status = ExecutionStatus.Completed;
        CompletedUtc = DateTime.UtcNow;
        DurationMilliseconds = (long)(CompletedUtc.Value - StartedUtc).TotalMilliseconds;
    }

    /// <summary>Marks this execution run failed with exception details.</summary>
    public void MarkFailed(string errorMessage, string? stackTrace)
    {
        Status = ExecutionStatus.Failed;
        CompletedUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        ErrorStackTrace = stackTrace;
        DurationMilliseconds = (long)(CompletedUtc.Value - StartedUtc).TotalMilliseconds;
    }

    /// <summary>Marks this execution run rolled back.</summary>
    public void MarkRolledBack()
    {
        Status = ExecutionStatus.RolledBack;
        CompletedUtc = DateTime.UtcNow;
        DurationMilliseconds = (long)(CompletedUtc.Value - StartedUtc).TotalMilliseconds;
    }
}
