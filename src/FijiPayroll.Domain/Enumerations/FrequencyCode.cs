namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the schedule codes for payroll frequency runs.
/// </summary>
public enum FrequencyCode
{
    /// <summary>Weekly pay frequency. 52 pay periods per year.</summary>
    Weekly = 1,

    /// <summary>Fortnightly pay frequency. 26 pay periods per year.</summary>
    Fortnightly = 2,

    /// <summary>Bi-monthly pay frequency. 24 pay periods per year.</summary>
    BiMonthly = 3,

    /// <summary>Monthly pay frequency. 12 pay periods per year.</summary>
    Monthly = 4,

    /// <summary>Custom pay frequency schedule.</summary>
    Custom = 5
}
