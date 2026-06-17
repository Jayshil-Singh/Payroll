namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Configured intervals for the BackgroundScheduler engine.
/// </summary>
public enum SchedulerInterval
{
    /// <summary>Trigger action every day.</summary>
    Daily = 1,

    /// <summary>Trigger action every week.</summary>
    Weekly = 2,

    /// <summary>Trigger action every two weeks.</summary>
    Fortnightly = 3,

    /// <summary>Trigger action every month.</summary>
    Monthly = 4,

    /// <summary>Trigger action every three months.</summary>
    Quarterly = 5,

    /// <summary>Trigger action every year.</summary>
    Yearly = 6,

    /// <summary>Action triggers only when manually run by an administrator.</summary>
    Manual = 7
}
