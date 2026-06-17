namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the lifecycle status of a payroll component or rule.
/// </summary>
public enum ComponentStatus
{
    /// <summary>
    /// Component is in draft and not used in calculations.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Component is active and included in calculations.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Component is inactive and excluded from calculations.
    /// </summary>
    Inactive = 3,

    /// <summary>
    /// Component is archived for historical auditing and read-only.
    /// </summary>
    Archived = 4
}
