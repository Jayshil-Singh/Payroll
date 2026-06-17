using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a calculated run period schedule mapping payment and cutoff boundaries.
/// </summary>
public sealed class PayPeriodSchedule : SoftDeleteEntity
{
    private PayPeriodSchedule() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the parent frequency configuration identifier.</summary>
    public int PayrollFrequencyDefinitionId { get; private set; }

    /// <summary>Gets the period sequence number within the calendar year.</summary>
    public int PeriodNumber { get; private set; }

    /// <summary>Gets the start date of this pay period cycle.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the end date of this pay period cycle.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Gets the timesheet/updates cutoff date for this period.</summary>
    public DateTime CutoffDate { get; private set; }

    /// <summary>Gets the target bank payment disbursement date.</summary>
    public DateTime PaymentDate { get; private set; }

    /// <summary>Gets a value indicating whether this pay period has been processed by the payroll runs engine.</summary>
    public bool IsProcessed { get; private set; }

    /// <summary>Factory method to create a new PayPeriodSchedule.</summary>
    public static PayPeriodSchedule Create(
        int companyId,
        int frequencyDefinitionId,
        int periodNumber,
        DateTime startDate,
        DateTime endDate,
        DateTime cutoffDate,
        DateTime paymentDate)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (frequencyDefinitionId <= 0)
            throw new ArgumentException("Frequency definition ID must be positive.", nameof(frequencyDefinitionId));
        if (periodNumber <= 0)
            throw new ArgumentException("Period number must be positive.", nameof(periodNumber));
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date.", nameof(startDate));
        if (cutoffDate > endDate)
            throw new ArgumentException("Cutoff date must be on or before end date.", nameof(cutoffDate));

        return new PayPeriodSchedule
        {
            CompanyId = companyId,
            PayrollFrequencyDefinitionId = frequencyDefinitionId,
            PeriodNumber = periodNumber,
            StartDate = startDate,
            EndDate = endDate,
            CutoffDate = cutoffDate,
            PaymentDate = paymentDate,
            IsProcessed = false
        };
    }

    /// <summary>Marks this pay period schedule as processed.</summary>
    public void MarkProcessed()
    {
        IsProcessed = true;
    }
}
