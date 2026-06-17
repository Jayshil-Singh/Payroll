using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a statutory compliance period (typically a monthly reporting window).
/// Enforces business constraints such as single-active-period rules.
/// </summary>
public sealed class CompliancePeriod : AuditableEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the reporting month (1-12).</summary>
    public int Month { get; private set; }

    /// <summary>Gets the reporting year (e.g. 2026).</summary>
    public int Year { get; private set; }

    /// <summary>Gets the starting date of the reporting period.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the ending date of the reporting period.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Gets the active status of this reporting period.</summary>
    public CompliancePeriodStatus Status { get; private set; }

    private CompliancePeriod() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new CompliancePeriod.
    /// </summary>
    public static CompliancePeriod Create(int companyId, int month, int year, DateTime startDate, DateTime endDate)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year), "Year must be a valid four-digit value.");
        if (startDate >= endDate) throw new ArgumentException("Start date must be before end date.");

        return new CompliancePeriod
        {
            CompanyId = companyId,
            Month = month,
            Year = year,
            StartDate = startDate,
            EndDate = endDate,
            Status = CompliancePeriodStatus.Open
        };
    }

    /// <summary>Locks the compliance period to prevent new runs but allow corrections.</summary>
    public void Lock()
    {
        if (Status != CompliancePeriodStatus.Open)
        {
            throw new InvalidOperationException("Only open periods can be locked.");
        }
        Status = CompliancePeriodStatus.Locked;
    }

    /// <summary>Unlocks a locked period back to open.</summary>
    public void Unlock()
    {
        if (Status != CompliancePeriodStatus.Locked)
        {
            throw new InvalidOperationException("Only locked periods can be unlocked.");
        }
        Status = CompliancePeriodStatus.Open;
    }

    /// <summary>Finalizes and closes the compliance period; no further changes are possible.</summary>
    public void Close()
    {
        if (Status != CompliancePeriodStatus.Locked)
        {
            throw new InvalidOperationException("Periods must be locked before they can be closed.");
        }
        Status = CompliancePeriodStatus.Closed;
    }
}
