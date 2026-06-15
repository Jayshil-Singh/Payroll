using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;

/// <summary>
/// Lightweight DTO for rendering a row in the Payroll Components list grid.
/// Contains only the fields shown in the list columns per Phase05-Configuration.md §9.1.
/// </summary>
public sealed record PayrollComponentSummaryDto
{
    /// <summary>Primary key.</summary>
    public int Id { get; init; }

    /// <summary>Unique component code.</summary>
    public string ComponentCode { get; init; } = string.Empty;

    /// <summary>Display name.</summary>
    public string ComponentName { get; init; } = string.Empty;

    /// <summary>Component type (Earning, Deduction, Allowance, Benefit, Statutory).</summary>
    public ComponentType ComponentType { get; init; }

    /// <summary>Human-readable component type label for display.</summary>
    public string ComponentTypeLabel => ComponentType.ToString();

    /// <summary>Calculation method (Fixed, Percentage, Formula, Manual).</summary>
    public CalculationMethod CalculationMethod { get; init; }

    /// <summary>Human-readable calculation method label for display.</summary>
    public string CalculationMethodLabel => CalculationMethod.ToString();

    /// <summary>Whether this component contributes to PAYE taxable income.</summary>
    public bool IsTaxable { get; init; }

    /// <summary>Whether this component contributes to FNPF-applicable gross.</summary>
    public bool IsFnpfApplicable { get; init; }

    /// <summary>Sort order on payslips.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>Whether this is a built-in system component.</summary>
    public bool IsSystemComponent { get; init; }

    /// <summary>Whether this component is currently active.</summary>
    public bool IsActive { get; init; }
}
