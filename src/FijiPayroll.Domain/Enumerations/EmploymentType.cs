namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the type of employment.
/// </summary>
public enum EmploymentType
{
    /// <summary>Permanent employee.</summary>
    Permanent = 1,

    /// <summary>Contractual employee with fixed duration.</summary>
    Contract = 2,

    /// <summary>Casual hourly paid employee.</summary>
    Casual = 3,

    /// <summary>Temporary short-term employee.</summary>
    Temporary = 4,

    /// <summary>Apprentice/trainee employee.</summary>
    Apprentice = 5
}
