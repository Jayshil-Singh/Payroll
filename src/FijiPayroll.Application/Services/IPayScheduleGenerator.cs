using FijiPayroll.Domain.Entities.Company;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service interface for generating pay period schedules.
/// </summary>
public interface IPayScheduleGenerator
{
    /// <summary>
    /// Generates a list of pay period schedules for a given frequency definition and fiscal calendar.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="frequencyDefinition">The payroll frequency definition configuration.</param>
    /// <param name="fiscalCalendar">The fiscal calendar configuration.</param>
    /// <returns>A task representing the asynchronous generation, returning a read-only list of generated PayPeriodSchedule.</returns>
    Task<IReadOnlyList<PayPeriodSchedule>> GenerateSchedulesAsync(
        int companyId,
        PayrollFrequencyDefinition frequencyDefinition,
        FiscalCalendar fiscalCalendar);
}
