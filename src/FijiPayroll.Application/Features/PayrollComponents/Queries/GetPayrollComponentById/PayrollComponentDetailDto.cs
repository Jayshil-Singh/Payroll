using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentById;

/// <summary>
/// Data Transfer Object for a full payroll component detail view.
/// Returned by <see cref="GetPayrollComponentByIdQueryHandler"/> and displayed
/// in the <c>PayrollComponentFormViewModel</c> edit form.
/// </summary>
public sealed record PayrollComponentDetailDto
{
    /// <summary>Primary key.</summary>
    public int Id { get; init; }

    /// <summary>Owning company ID.</summary>
    public int CompanyId { get; init; }

    /// <summary>Unique component code within the company.</summary>
    public string ComponentCode { get; init; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string ComponentName { get; init; } = string.Empty;

    /// <summary>Component classification.</summary>
    public ComponentType ComponentType { get; init; }

    /// <summary>Human-readable component type label.</summary>
    public string ComponentTypeLabel => ComponentType.ToString();

    /// <summary>How the value is determined.</summary>
    public CalculationMethod CalculationMethod { get; init; }

    /// <summary>Human-readable calculation method label.</summary>
    public string CalculationMethodLabel => CalculationMethod.ToString();

    /// <summary>Fixed amount or percentage value. Null for Formula and Manual.</summary>
    public decimal? CalculationValue { get; init; }

    /// <summary>Formula expression string. Null for non-formula components.</summary>
    public string? Formula { get; init; }

    /// <summary>Whether the component contributes to PAYE taxable income.</summary>
    public bool IsTaxable { get; init; }

    /// <summary>Whether the component contributes to FNPF-applicable gross.</summary>
    public bool IsFnpfApplicable { get; init; }

    /// <summary>Sort order on payslips.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Whether this is a built-in system component (PAYE, FNPF).</summary>
    public bool IsSystemComponent { get; init; }

    /// <summary>Whether this component is active and included in payroll runs.</summary>
    public bool IsActive { get; init; }

    /// <summary>Username who created this record.</summary>
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Username of the last modifier. Null if never modified.</summary>
    public string? ModifiedBy { get; init; }

    /// <summary>UTC timestamp of last modification.</summary>
    public DateTime? ModifiedAt { get; init; }
}
