using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service implementation of the IFiscalCalendarGenerator.
/// Handles weekly, fortnightly, monthly, and custom calendar period generations.
/// </summary>
public sealed class FiscalCalendarGenerator : IFiscalCalendarGenerator
{
    /// <inheritdoc />
    public Task<FiscalCalendar> GenerateCalendarAsync(int companyId, int fiscalYear, DateTime startDate, CalendarType calendarType, string generatedBy)
    {
        // Enforce start date UTC conversion
        var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc).Date;
        var calendarEndDate = utcStartDate.AddYears(1).AddDays(-1);

        var calendar = FiscalCalendar.Create(companyId, fiscalYear, utcStartDate, calendarEndDate, calendarType, generatedBy);

        switch (calendarType)
        {
            case CalendarType.Weekly:
                GenerateWeeklyPeriods(calendar);
                break;
            case CalendarType.Fortnightly:
                GenerateFortnightlyPeriods(calendar);
                break;
            case CalendarType.Monthly:
            case CalendarType.Custom:
            default:
                GenerateMonthlyPeriods(calendar);
                break;
        }

        return Task.FromResult(calendar);
    }

    private void GenerateWeeklyPeriods(FiscalCalendar calendar)
    {
        int periodNumber = 1;
        var currentStart = calendar.StartDate;

        while (currentStart <= calendar.EndDate)
        {
            var currentEnd = currentStart.AddDays(6);
            if (currentEnd > calendar.EndDate)
            {
                currentEnd = calendar.EndDate;
            }

            var periodName = $"Week {periodNumber}";
            var period = FiscalPeriod.Create(calendar.CompanyId, 0, periodNumber, periodName, currentStart, currentEnd);
            calendar.AddPeriod(period);

            periodNumber++;
            currentStart = currentStart.AddDays(7);
        }
    }

    private void GenerateFortnightlyPeriods(FiscalCalendar calendar)
    {
        int periodNumber = 1;
        var currentStart = calendar.StartDate;

        while (currentStart <= calendar.EndDate)
        {
            var currentEnd = currentStart.AddDays(13);
            if (currentEnd > calendar.EndDate)
            {
                currentEnd = calendar.EndDate;
            }

            var periodName = $"Fortnight {periodNumber}";
            var period = FiscalPeriod.Create(calendar.CompanyId, 0, periodNumber, periodName, currentStart, currentEnd);
            calendar.AddPeriod(period);

            periodNumber++;
            currentStart = currentStart.AddDays(14);
        }
    }

    private void GenerateMonthlyPeriods(FiscalCalendar calendar)
    {
        for (int i = 1; i <= 12; i++)
        {
            var currentStart = calendar.StartDate.AddMonths(i - 1);
            var currentEnd = calendar.StartDate.AddMonths(i).AddDays(-1);

            if (currentEnd > calendar.EndDate)
            {
                currentEnd = calendar.EndDate;
            }

            var periodName = currentStart.ToString("MMMM yyyy");
            var period = FiscalPeriod.Create(calendar.CompanyId, 0, i, periodName, currentStart, currentEnd);
            calendar.AddPeriod(period);
        }
    }
}
