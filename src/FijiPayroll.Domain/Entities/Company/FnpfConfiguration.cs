using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing FNPF contribution configuration rates and rules for a company tenant.
/// </summary>
public sealed class FnpfConfiguration : SoftDeleteEntity
{
    private FnpfConfiguration() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the employer contribution rate (e.g. 0.10 for 10%).</summary>
    public decimal EmployerRate { get; private set; }

    /// <summary>Gets the employee contribution rate (e.g. 0.08 for 8%).</summary>
    public decimal EmployeeRate { get; private set; }

    /// <summary>Gets the date when this configuration becomes effective.</summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>Gets a value indicating whether this configuration is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Factory method to create a new FnpfConfiguration.</summary>
    public static FnpfConfiguration Create(
        int companyId,
        decimal employerRate,
        decimal employeeRate,
        DateTime effectiveDate,
        bool isActive = true)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (employerRate < 0 || employerRate > 1)
            throw new ArgumentOutOfRangeException(nameof(employerRate), "Employer rate must be between 0 and 1.");
        if (employeeRate < 0 || employeeRate > 1)
            throw new ArgumentOutOfRangeException(nameof(employeeRate), "Employee rate must be between 0 and 1.");

        return new FnpfConfiguration
        {
            CompanyId = companyId,
            EmployerRate = employerRate,
            EmployeeRate = employeeRate,
            EffectiveDate = effectiveDate,
            IsActive = isActive
        };
    }

    /// <summary>Updates the rate configurations.</summary>
    public void UpdateRates(decimal employerRate, decimal employeeRate, DateTime effectiveDate, bool isActive)
    {
        if (employerRate < 0 || employerRate > 1)
            throw new ArgumentOutOfRangeException(nameof(employerRate), "Employer rate must be between 0 and 1.");
        if (employeeRate < 0 || employeeRate > 1)
            throw new ArgumentOutOfRangeException(nameof(employeeRate), "Employee rate must be between 0 and 1.");

        EmployerRate = employerRate;
        EmployeeRate = employeeRate;
        EffectiveDate = effectiveDate;
        IsActive = isActive;
    }
}
