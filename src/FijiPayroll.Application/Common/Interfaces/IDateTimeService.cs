namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Provides the current UTC date and time, abstracted for testability.
/// Implementations inject <c>DateTime.UtcNow</c> in production
/// and a fixed value in unit tests.
/// </summary>
public interface IDateTimeService
{
    /// <summary>Current date and time in UTC.</summary>
    DateTime UtcNow { get; }

    /// <summary>Current date in UTC (time portion stripped).</summary>
    DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
