namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Policy options for handling voluntary deductions when an employee's net pay is insufficient.
/// </summary>
public enum VoluntaryDeductionPolicy
{
    /// <summary>
    /// Policy A: Block payroll run calculation if net pay is negative after voluntary deductions.
    /// Throws a PayrollException.
    /// </summary>
    BlockPayroll = 1,

    /// <summary>
    /// Policy B: Apply voluntary deductions partially and carry forward the remainder.
    /// </summary>
    CarryForwardRemainder = 2,

    /// <summary>
    /// Policy C: Apply voluntary deductions partially and raise an audit flag/warning.
    /// </summary>
    PartialDeductionWithAuditFlag = 3
}
