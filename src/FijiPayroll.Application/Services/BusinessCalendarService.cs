using System;
using System.Collections.Generic;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service providing unified calendar and period functions for Fiji Enterprise platform.
/// Handles public holidays, weekends, leave days, and fiscal tax periods.
/// </summary>
public sealed class BusinessCalendarService
{
    private static readonly HashSet<DateTime> FijiHolidays2026 = new()
    {
        new DateTime(2026, 1, 1),   // New Year's Day
        new DateTime(2026, 4, 3),   // Good Friday
        new DateTime(2026, 4, 4),   // Easter Saturday
        new DateTime(2026, 4, 6),   // Easter Monday
        new DateTime(2026, 6, 15),  // Constitution Day
        new DateTime(2026, 9, 7),   // Prophet Mohammed's Birthday
        new DateTime(2026, 10, 12), // Fiji Day
        new DateTime(2026, 11, 9),  // Diwali
        new DateTime(2026, 12, 25), // Christmas Day
        new DateTime(2026, 12, 26)  // Boxing Day
    };

    /// <summary>
    /// Gets whether a date is a public holiday in Fiji.
    /// </summary>
    public bool IsPublicHoliday(DateTime date)
    {
        return FijiHolidays2026.Contains(date.Date);
    }

    /// <summary>
    /// Gets whether a date is a weekend day.
    /// </summary>
    public bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>
    /// Calculates working days between start and end dates.
    /// </summary>
    public int GetWorkingDays(DateTime start, DateTime end)
    {
        int count = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (!IsWeekend(date) && !IsPublicHoliday(date))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Determines the financial year for a given date.
    /// </summary>
    public int GetFinancialYear(DateTime date)
    {
        // Fiji tax year is standard calendar year (Jan 1 to Dec 31)
        return date.Year;
    }

    /// <summary>
    /// Determines if a date falls in a given FNPF period.
    /// </summary>
    public string GetFnpfPeriodCode(DateTime date)
    {
        return date.ToString("yyyy-MM");
    }

    /// <summary>
    /// Determines if a date falls in a given FRCS tax period.
    /// </summary>
    public string GetFrcsPeriodCode(DateTime date)
    {
        return date.ToString("yyyy-MM");
    }
}
