namespace FijiPayroll.Shared.Extensions;

/// <summary>
/// Extension methods for <see cref="decimal"/> values, providing
/// Fiji payroll-specific formatting and rounding utilities.
/// </summary>
public static class DecimalExtensions
{
    /// <summary>
    /// Rounds a monetary value to 2 decimal places using midpoint rounding
    /// away from zero, as required by the FRCS rounding policy.
    /// </summary>
    /// <param name="value">The monetary amount.</param>
    /// <returns>Amount rounded to 2 decimal places.</returns>
    public static decimal RoundMoney(this decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Standardised rounding matching live payroll calculations (MidpointRounding.AwayFromZero).
    /// </summary>
    public static decimal ToFijiRound(this decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Rounds a monetary value to 4 decimal places for internal storage,
    /// used for gross pay and intermediate calculations.
    /// </summary>
    /// <param name="value">The monetary amount.</param>
    /// <returns>Amount rounded to 4 decimal places.</returns>
    public static decimal RoundMoneyStorage(this decimal value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Rounds a leave accrual quantity to 4 decimal places.
    /// </summary>
    public static decimal RoundLeaveAccrual(this decimal value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Formats a decimal as a Fiji Dollar currency string (e.g., "$1,234.56").
    /// </summary>
    public static string ToFjdString(this decimal value)
        => $"${value:N2}";

    /// <summary>
    /// Returns <c>true</c> if the value is greater than zero.
    /// </summary>
    public static bool IsPositive(this decimal value) => value > 0m;

    /// <summary>
    /// Returns <c>true</c> if the value is zero or negative.
    /// </summary>
    public static bool IsZeroOrNegative(this decimal value) => value <= 0m;

    /// <summary>
    /// Converts a percentage value (e.g., 8.0) to its decimal fraction equivalent (0.08).
    /// </summary>
    public static decimal ToFraction(this decimal percentage) => percentage / 100m;
}
