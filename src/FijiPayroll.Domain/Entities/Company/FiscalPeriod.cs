using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a specific month/period breakdown inside a fiscal year.
/// </summary>
public sealed class FiscalPeriod : SoftDeleteEntity
{
    private FiscalPeriod() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the parent fiscal calendar identifier.</summary>
    public int FiscalCalendarId { get; private set; }

    /// <summary>Gets the sequence number of this period within the year.</summary>
    public int PeriodNumber { get; private set; }

    /// <summary>Gets the display name of this period (e.g. "January 2026").</summary>
    public string PeriodName { get; private set; } = string.Empty;

    /// <summary>Gets the start date of this period.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the end date of this period.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Gets a value indicating whether this period is closed.</summary>
    public bool IsClosed { get; private set; }

    /// <summary>Factory method to build a new FiscalPeriod.</summary>
    public static FiscalPeriod Create(
        int companyId,
        int fiscalCalendarId,
        int periodNumber,
        string periodName,
        DateTime startDate,
        DateTime endDate)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (fiscalCalendarId <= 0)
            throw new ArgumentException("Fiscal calendar ID must be positive.", nameof(fiscalCalendarId));
        if (periodNumber <= 0)
            throw new ArgumentException("Period number must be positive.", nameof(periodNumber));
        if (string.IsNullOrWhiteSpace(periodName))
            throw new ArgumentException("Period name cannot be empty.", nameof(periodName));
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date.", nameof(startDate));

        return new FiscalPeriod
        {
            CompanyId = companyId,
            FiscalCalendarId = fiscalCalendarId,
            PeriodNumber = periodNumber,
            PeriodName = periodName,
            StartDate = startDate,
            EndDate = endDate,
            IsClosed = false
        };
    }

    /// <summary>Closes this fiscal period.</summary>
    public void ClosePeriod()
    {
        IsClosed = true;
    }
}
