using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service interface for generating fiscal calendars and period breakdowns.
/// </summary>
public interface IFiscalCalendarGenerator
{
    /// <summary>
    /// Generates a new FiscalCalendar with periods breakdown based on calendar type.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="fiscalYear">The fiscal year number (e.g. 2026).</param>
    /// <param name="startDate">The start date of the fiscal year.</param>
    /// <param name="calendarType">The calendar division structure type.</param>
    /// <param name="generatedBy">The username of the generating administrator.</param>
    /// <returns>A task representing the asynchronous generation, returning the generated FiscalCalendar.</returns>
    Task<FiscalCalendar> GenerateCalendarAsync(int companyId, int fiscalYear, DateTime startDate, CalendarType calendarType, string generatedBy);
}
