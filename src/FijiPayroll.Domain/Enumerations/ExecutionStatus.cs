namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the lifecycle status states of a wizard setup or background orchestrator execution run.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>The execution task is queued or waiting to begin.</summary>
    Pending = 1,

    /// <summary>The execution task is currently active and processing.</summary>
    Running = 2,

    /// <summary>The execution task completed successfully and committed change records.</summary>
    Completed = 3,

    /// <summary>The execution task failed and logged error reports.</summary>
    Failed = 4,

    /// <summary>The execution transaction was aborted and fully rolled back to prevent data corruption.</summary>
    RolledBack = 5,

    /// <summary>The execution task failed and is retrying connection attempts.</summary>
    Retrying = 6,

    /// <summary>The execution task was manually canceled by the administrator.</summary>
    Cancelled = 7
}
