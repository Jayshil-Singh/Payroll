namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Severity level of a payroll validation message or exception.
/// </summary>
public enum PayrollValidationSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
