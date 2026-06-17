namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Severity level of issues caught by compliance validators.
/// </summary>
public enum ComplianceValidatorSeverity
{
    /// <summary>Informational note; does not restrict submission processing.</summary>
    Info = 1,

    /// <summary>Warning check; should be reviewed but can be submitted.</summary>
    Warning = 2,

    /// <summary>Error block; must be corrected before file generation or submission is permitted.</summary>
    Error = 3
}
