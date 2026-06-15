using System.Runtime.CompilerServices;

namespace FijiPayroll.Shared.Guards;

/// <summary>
/// Provides defensive guard clauses to validate arguments and state
/// before executing business logic. Throws standard .NET exceptions
/// with descriptive messages without relying on external libraries.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">Any reference type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Automatically inferred from the caller expression.</param>
    /// <returns>The non-null value.</returns>
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is <c>null</c>,
    /// empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="paramName">Automatically inferred from the caller expression.</param>
    /// <returns>The trimmed, non-empty string.</returns>
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{paramName}' must not be null or white-space.", paramName);
        }

        return value.Trim();
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/>
    /// is less than or equal to zero.
    /// </summary>
    /// <param name="value">The decimal value to check.</param>
    /// <param name="paramName">Automatically inferred from the caller expression.</param>
    /// <returns>The validated positive value.</returns>
    public static decimal AgainstNegativeOrZero(
        decimal value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be greater than zero.");
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/>
    /// is less than zero.
    /// </summary>
    public static decimal AgainstNegative(
        decimal value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must not be negative.");
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/>
    /// exceeds <paramref name="maxLength"/> characters.
    /// </summary>
    public static string AgainstMaxLength(
        string value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value.Length,
                $"'{paramName}' must not exceed {maxLength} characters. Received {value.Length}.");
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is not
    /// a defined member of the enumeration <typeparamref name="TEnum"/>.
    /// </summary>
    public static TEnum AgainstInvalidEnum<TEnum>(
        TEnum value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a valid value for enum '{typeof(TEnum).Name}'.",
                paramName);
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> with <paramref name="message"/>
    /// if <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    public static void AgainstCondition(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
