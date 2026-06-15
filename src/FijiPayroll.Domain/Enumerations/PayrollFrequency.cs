namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the payroll frequency — how often employees in a pay group are paid.
/// Used to determine the number of pay periods per year and therefore
/// the annualisation factor in PAYE calculations.
/// </summary>
public enum PayrollFrequency
{
    /// <summary>Paid every 7 days. 52 periods per year.</summary>
    Weekly = 1,

    /// <summary>Paid every 14 days. 26 periods per year.</summary>
    Fortnightly = 2,

    /// <summary>
    /// Paid twice per month (semi-monthly), typically on the 15th and last day.
    /// 24 periods per year.
    /// </summary>
    BiMonthly = 3,

    /// <summary>Paid once per calendar month. 12 periods per year.</summary>
    Monthly = 4,
}
