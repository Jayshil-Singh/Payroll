namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the categories of system reference data seeded during installation and database upgrades.
/// </summary>
public enum SeedCategory
{
    /// <summary>Global bank register seeds.</summary>
    Banks = 1,

    /// <summary>Global branch register seeds.</summary>
    Branches = 2,

    /// <summary>Fiji standard leave type entitlement configurations.</summary>
    LeaveTypes = 3,

    /// <summary>Mandatory payroll components (BASIC, PAYE, FNPF-EMP, FNPF-EMPLR).</summary>
    PayrollComponents = 4,

    /// <summary>SSRS reports registries paths seeds.</summary>
    Reports = 5,

    /// <summary>Security role profiles classifications.</summary>
    Roles = 6,

    /// <summary>Permitted action permission codes mappings.</summary>
    Permissions = 7,

    /// <summary>System defaults settings records.</summary>
    Settings = 8
}
