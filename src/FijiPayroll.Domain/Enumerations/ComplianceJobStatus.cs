namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Execution states of asynchronous compliance processing background jobs.
/// </summary>
public enum ComplianceJobStatus
{
    /// <summary>Job is scheduled and awaiting thread activation.</summary>
    Pending = 1,

    /// <summary>Job is currently processing on an active thread.</summary>
    Running = 2,

    /// <summary>Job finished successfully.</summary>
    Completed = 3,

    /// <summary>Job failed with an exception.</summary>
    Failed = 4,

    /// <summary>Job failed and is waiting to retry.</summary>
    Retrying = 5,

    /// <summary>Job was explicitly cancelled by an administrator.</summary>
    Cancelled = 6
}
