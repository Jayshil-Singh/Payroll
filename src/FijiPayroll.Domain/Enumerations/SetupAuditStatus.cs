namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the relative status states of a setup onboarding audit trail step.
/// </summary>
public enum SetupAuditStatus
{
    /// <summary>The step audited completed cleanly with zero warnings.</summary>
    Success = 1,

    /// <summary>The step audited completed with minor validation warnings.</summary>
    Warning = 2,

    /// <summary>The step audited failed to pass validation checks.</summary>
    Failed = 3
}
