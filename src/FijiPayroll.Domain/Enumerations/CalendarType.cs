namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the structure type of a fiscal calendar.
/// </summary>
public enum CalendarType
{
    /// <summary>Calendar consisting of 52 or 53 weekly pay periods.</summary>
    Weekly = 1,

    /// <summary>Calendar consisting of 26 or 27 fortnightly pay periods.</summary>
    Fortnightly = 2,

    /// <summary>Standard calendar consisting of 12 monthly periods.</summary>
    Monthly = 3,

    /// <summary>Custom calendar structure for non-standard fiscal schedules.</summary>
    Custom = 4
}
