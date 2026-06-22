using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Represents a Payroll Period for managing lifecycle states of processing cycles.
/// </summary>
public sealed class PayrollPeriod : AuditableEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the unique code for this period (e.g. "2026-M06").</summary>
    public string PeriodCode { get; private set; } = string.Empty;

    /// <summary>Gets the frequency of this period.</summary>
    public PayrollFrequencyType PayrollFrequency { get; private set; }

    /// <summary>Gets the fiscal year of the period.</summary>
    public int FiscalYear { get; private set; }

    /// <summary>Gets the fiscal month of the period.</summary>
    public int FiscalMonth { get; private set; }

    /// <summary>Gets the start date of the period.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the end date of the period.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Gets the payment or disbursement date of the period.</summary>
    public DateTime PaymentDate { get; private set; }

    /// <summary>Gets the current status of the payroll period.</summary>
    public PayrollPeriodStatus Status { get; private set; }

    private PayrollPeriod() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new open Payroll Period.
    /// </summary>
    public static PayrollPeriod Create(
        int companyId,
        string periodCode,
        PayrollFrequencyType frequency,
        int fiscalYear,
        int fiscalMonth,
        DateTime startDate,
        DateTime endDate,
        DateTime paymentDate,
        string createdBy)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (string.IsNullOrWhiteSpace(periodCode)) throw new ArgumentException("Period code cannot be empty.", nameof(periodCode));
        if (startDate >= endDate) throw new ArgumentException("Start date must be before end date.");

        return new PayrollPeriod
        {
            CompanyId = companyId,
            PeriodCode = periodCode,
            PayrollFrequency = frequency,
            FiscalYear = fiscalYear,
            FiscalMonth = fiscalMonth,
            StartDate = startDate,
            EndDate = endDate,
            PaymentDate = paymentDate,
            Status = PayrollPeriodStatus.Open,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Transitions the period to a new status while enforcing validation constraints.
    /// </summary>
    public void UpdateStatus(PayrollPeriodStatus newStatus, string user)
    {
        if (Status == PayrollPeriodStatus.Locked)
        {
            throw new InvalidOperationException("Locked periods are immutable.");
        }
        if (Status == PayrollPeriodStatus.Archived)
        {
            throw new InvalidOperationException("Archived periods are read-only.");
        }

        if (newStatus == PayrollPeriodStatus.Locked && Status != PayrollPeriodStatus.Closed)
        {
            throw new InvalidOperationException("Cannot lock until period is closed.");
        }

        Status = newStatus;
        ModifiedBy = user;
        ModifiedAt = DateTime.UtcNow;
    }
}
