using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a company payroll frequency definition.
/// Resolves naming collisions with the original frequency settings.
/// </summary>
public sealed class PayrollFrequencyDefinition : SoftDeleteEntity
{
    private readonly List<PayPeriodSchedule> _schedules = new();

    private PayrollFrequencyDefinition() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the display name of the frequency configuration.</summary>
    public string FrequencyName { get; private set; } = string.Empty;

    /// <summary>Gets the system calculation type of the frequency.</summary>
    public PayrollFrequencyType FrequencyType { get; private set; } = PayrollFrequencyType.Monthly;

    /// <summary>Gets the custom schedule classification code.</summary>
    public FrequencyCode FrequencyCode { get; private set; } = FrequencyCode.Monthly;

    /// <summary>Gets the designated payday description (e.g. "Friday").</summary>
    public string PayDay { get; private set; } = string.Empty;

    /// <summary>Gets the total number of periods generated per year.</summary>
    public int PeriodsPerYear { get; private set; }

    /// <summary>Gets the optional details about the pay group.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this frequency configuration is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the generated pay period schedules associated with this frequency.</summary>
    public IReadOnlyCollection<PayPeriodSchedule> Schedules => _schedules.AsReadOnly();

    /// <summary>Factory method to create a new PayrollFrequencyDefinition.</summary>
    public static PayrollFrequencyDefinition Create(
        int companyId,
        string name,
        PayrollFrequencyType type,
        FrequencyCode code,
        string payDay,
        int periodsPerYear,
        string description)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Frequency name is required.", nameof(name));
        if (periodsPerYear <= 0)
            throw new ArgumentException("Periods per year must be positive.", nameof(periodsPerYear));

        return new PayrollFrequencyDefinition
        {
            CompanyId = companyId,
            FrequencyName = name,
            FrequencyType = type,
            FrequencyCode = code,
            PayDay = payDay ?? string.Empty,
            PeriodsPerYear = periodsPerYear,
            Description = description ?? string.Empty,
            IsActive = true
        };
    }

    /// <summary>Associates a generated pay period schedule with this definition.</summary>
    public void AssociateSchedule(PayPeriodSchedule schedule)
    {
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));
        _schedules.Add(schedule);
    }
}
