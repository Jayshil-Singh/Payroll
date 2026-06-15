namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines how the value of a payroll component is determined during
/// payroll calculation. Corresponds to the CalculationMethod column in
/// <c>company.PayrollComponents</c>.
/// </summary>
public enum CalculationMethod
{
    /// <summary>
    /// A fixed monetary amount applied every pay period, regardless of
    /// the employee's base pay (e.g., $500 Housing Allowance per month).
    /// The <c>CalculationValue</c> column holds the dollar amount.
    /// </summary>
    Fixed = 1,

    /// <summary>
    /// A percentage of the relevant base (typically gross pay or FNPF-applicable gross).
    /// The <c>CalculationValue</c> column holds the percentage (e.g., 8.0 for 8%).
    /// </summary>
    Percentage = 2,

    /// <summary>
    /// A user-defined formula expression evaluated at runtime.
    /// Supported variables: <c>{GrossPay}</c>, <c>{AnnualSalary}</c>,
    /// <c>{HoursWorked}</c>, <c>{DailyRate}</c>, <c>{OvertimeHours}</c>.
    /// The <c>Formula</c> column holds the expression text.
    /// </summary>
    Formula = 3,

    /// <summary>
    /// The value is entered manually per employee, per pay period during data entry.
    /// No default calculation is applied. Used for irregular payments such as
    /// one-off bonuses or ad-hoc allowances.
    /// </summary>
    Manual = 4,
}
