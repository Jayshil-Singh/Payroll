using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service implementation of the IPayScheduleGenerator.
/// Generates periodic schedules with cutoffs and payment date alignments.
/// </summary>
public sealed class PayScheduleGenerator : IPayScheduleGenerator
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PayPeriodSchedule>> GenerateSchedulesAsync(
        int companyId,
        PayrollFrequencyDefinition frequencyDefinition,
        FiscalCalendar fiscalCalendar)
    {
        var schedules = new List<PayPeriodSchedule>();

        switch (frequencyDefinition.FrequencyType)
        {
            case PayrollFrequencyType.Weekly:
                GenerateWeeklySchedules(companyId, frequencyDefinition, fiscalCalendar, schedules);
                break;
            case PayrollFrequencyType.Fortnightly:
                GenerateFortnightlySchedules(companyId, frequencyDefinition, fiscalCalendar, schedules);
                break;
            case PayrollFrequencyType.Monthly:
            default:
                GenerateMonthlySchedules(companyId, frequencyDefinition, fiscalCalendar, schedules);
                break;
        }

        return Task.FromResult<IReadOnlyList<PayPeriodSchedule>>(schedules);
    }

    private void GenerateWeeklySchedules(
        int companyId,
        PayrollFrequencyDefinition definition,
        FiscalCalendar calendar,
        List<PayPeriodSchedule> schedules)
    {
        int limit = definition.PeriodsPerYear;
        var currentStart = calendar.StartDate;

        for (int i = 1; i <= limit; i++)
        {
            if (currentStart >= calendar.EndDate)
            {
                break;
            }

            var currentEnd = currentStart.AddDays(6);
            if (currentEnd > calendar.EndDate)
            {
                currentEnd = calendar.EndDate;
            }

            var cutoff = currentEnd.AddDays(-2);
            if (cutoff < currentStart)
            {
                cutoff = currentStart;
            }

            var paymentDate = CalculatePaymentDate(currentEnd, definition.PayDay, definition.FrequencyType);
            if (paymentDate > calendar.EndDate)
            {
                paymentDate = calendar.EndDate;
            }

            var schedule = PayPeriodSchedule.Create(companyId, definition.Id, i, currentStart, currentEnd, cutoff, paymentDate);
            schedules.Add(schedule);

            currentStart = currentStart.AddDays(7);
        }
    }

    private void GenerateFortnightlySchedules(
        int companyId,
        PayrollFrequencyDefinition definition,
        FiscalCalendar calendar,
        List<PayPeriodSchedule> schedules)
    {
        int limit = definition.PeriodsPerYear;
        var currentStart = calendar.StartDate;

        for (int i = 1; i <= limit; i++)
        {
            if (currentStart >= calendar.EndDate)
            {
                break;
            }

            var currentEnd = currentStart.AddDays(13);
            if (currentEnd > calendar.EndDate)
            {
                currentEnd = calendar.EndDate;
            }

            var cutoff = currentEnd.AddDays(-2);
            if (cutoff < currentStart)
            {
                cutoff = currentStart;
            }

            var paymentDate = CalculatePaymentDate(currentEnd, definition.PayDay, definition.FrequencyType);
            if (paymentDate > calendar.EndDate)
            {
                paymentDate = calendar.EndDate;
            }

            var schedule = PayPeriodSchedule.Create(companyId, definition.Id, i, currentStart, currentEnd, cutoff, paymentDate);
            schedules.Add(schedule);

            currentStart = currentStart.AddDays(14);
        }
    }

    private void GenerateMonthlySchedules(
        int companyId,
        PayrollFrequencyDefinition definition,
        FiscalCalendar calendar,
        List<PayPeriodSchedule> schedules)
    {
        int limit = Math.Min(definition.PeriodsPerYear, 12);

        for (int i = 1; i <= limit; i++)
        {
            var tempStart = calendar.StartDate.AddMonths(i - 1);
            if (tempStart >= calendar.EndDate)
            {
                break;
            }

            var currentEnd = calendar.StartDate.AddMonths(i).AddDays(-1);
            if (currentEnd > calendar.EndDate)
            {
                currentEnd = calendar.EndDate;
            }

            var cutoff = currentEnd.AddDays(-2);
            if (cutoff < tempStart)
            {
                cutoff = tempStart;
            }

            var paymentDate = CalculatePaymentDate(currentEnd, definition.PayDay, definition.FrequencyType);
            if (paymentDate > calendar.EndDate)
            {
                paymentDate = calendar.EndDate;
            }

            var schedule = PayPeriodSchedule.Create(companyId, definition.Id, i, tempStart, currentEnd, cutoff, paymentDate);
            schedules.Add(schedule);
        }
    }

    private static DateTime CalculatePaymentDate(DateTime endDate, string payDay, PayrollFrequencyType freqType)
    {
        if (string.IsNullOrWhiteSpace(payDay))
        {
            return endDate;
        }

        // Check if day of week (e.g. "Friday")
        if (Enum.TryParse<DayOfWeek>(payDay, true, out var targetDay))
        {
            var paymentDate = endDate;
            while (paymentDate.DayOfWeek != targetDay)
            {
                paymentDate = paymentDate.AddDays(1);
            }
            return paymentDate;
        }

        // Check if monthly numeric day (e.g. "25")
        if (freqType == PayrollFrequencyType.Monthly && int.TryParse(payDay, out var dayNum))
        {
            try
            {
                int year = endDate.Year;
                int month = endDate.Month;
                int maxDays = DateTime.DaysInMonth(year, month);
                int resolvedDay = Math.Clamp(dayNum, 1, maxDays);
                return new DateTime(year, month, resolvedDay, 0, 0, 0, DateTimeKind.Utc);
            }
            catch
            {
                return endDate;
            }
        }

        return endDate;
    }
}
