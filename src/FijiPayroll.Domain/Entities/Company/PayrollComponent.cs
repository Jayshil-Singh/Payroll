using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Shared.Constants;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a configurable payroll component (earnings, deduction, allowance, benefit,
/// or statutory item) that is applied during payroll calculation.
///
/// Maps to the <c>company.PayrollComponents</c> table as defined in Database.md §5.6.
///
/// Business rules enforced by this entity:
/// <list type="bullet">
///   <item>System components (PAYE, FNPF) cannot be deleted or deactivated.</item>
///   <item>A Formula component must have a non-empty Formula expression.</item>
///   <item>A Fixed or Percentage component must have a positive CalculationValue.</item>
///   <item>ComponentCode is always stored in uppercase, max 20 characters.</item>
///   <item>DisplayOrder must be non-negative.</item>
/// </list>
/// </summary>
public sealed class PayrollComponent : SoftDeleteEntity
{
    // ─── Private backing fields ──────────────────────────────────────────────────

    private string _componentCode = string.Empty;
    private string _componentName = string.Empty;
    private string? _formula;
    private decimal? _calculationValue;
    private int _displayOrder;

    // ─── Private constructor (use factory method) ────────────────────────────────

    private PayrollComponent() { }

    // ─── Public properties ───────────────────────────────────────────────────────

    /// <summary>
    /// Foreign key to <c>company.Companies</c>. Every component belongs to exactly one company.
    /// </summary>
    public int CompanyId { get; private set; }

    /// <summary>
    /// Short alphanumeric code uniquely identifying this component within the company
    /// (e.g., "BASIC", "HRA", "PAYE"). Always uppercase. Max 20 characters.
    /// </summary>
    public string ComponentCode
    {
        get => _componentCode;
        private set => _componentCode = Guard.AgainstNullOrWhiteSpace(value).ToUpperInvariant();
    }

    /// <summary>
    /// Human-readable display name shown on payslips and reports (e.g., "Housing Allowance").
    /// Max 200 characters.
    /// </summary>
    public string ComponentName
    {
        get => _componentName;
        private set => _componentName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Classifies the component as an Earning, Deduction, Allowance, Benefit, or Statutory item.
    /// </summary>
    public ComponentType ComponentType { get; private set; }

    /// <summary>
    /// Determines how the component amount is computed during payroll calculation.
    /// </summary>
    public CalculationMethod CalculationMethod { get; private set; }

    /// <summary>
    /// The dollar amount (for <see cref="CalculationMethod.Fixed"/>) or percentage
    /// (for <see cref="CalculationMethod.Percentage"/>). Null for Formula and Manual methods.
    /// </summary>
    public decimal? CalculationValue
    {
        get => _calculationValue;
        private set
        {
            if (value.HasValue)
            {
                Guard.AgainstNegativeOrZero(value.Value);
            }

            _calculationValue = value;
        }
    }

    /// <summary>
    /// For <see cref="CalculationMethod.Formula"/> components — the expression string.
    /// Supported variables: {GrossPay}, {AnnualSalary}, {HoursWorked}, {DailyRate}, {OvertimeHours}.
    /// Null for all other calculation methods.
    /// </summary>
    public string? Formula
    {
        get => _formula;
        private set => _formula = value?.Trim();
    }

    /// <summary>
    /// When <c>true</c>, this component is a built-in system component (PAYE, FNPF)
    /// that cannot be deleted or deactivated.
    /// </summary>
    public bool IsSystemComponent { get; private set; }

    /// <summary>
    /// When <c>true</c>, this component's value is included in the taxable income
    /// used for PAYE calculation.
    /// </summary>
    public bool IsTaxable { get; private set; }

    /// <summary>
    /// When <c>true</c>, this component's value is included in the FNPF-applicable
    /// gross used to calculate employee and employer FNPF contributions.
    /// </summary>
    public bool IsFnpfApplicable { get; private set; }

    /// <summary>
    /// Controls the order in which this component appears on payslips and reports.
    /// Lower numbers appear first. Must be non-negative.
    /// </summary>
    public int DisplayOrder
    {
        get => _displayOrder;
        private set
        {
            if (value < 0)
            {
                throw new DomainException("DisplayOrder must be a non-negative integer.");
            }

            _displayOrder = value;
        }
    }

    /// <summary>
    /// Optional long-form description explaining the purpose or calculation logic.
    /// Max 500 characters.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// When <c>false</c>, this component is excluded from all payroll calculations.
    /// System components cannot be set to inactive.
    /// </summary>
    public bool IsActive { get; private set; }

    // ─── Factory Method ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates and validates a new <see cref="PayrollComponent"/> instance.
    /// </summary>
    /// <param name="companyId">The owning company.</param>
    /// <param name="componentCode">Unique code within the company (max 20 chars, uppercase).</param>
    /// <param name="componentName">Human-readable name (max 200 chars).</param>
    /// <param name="componentType">Classification (Earning, Deduction, Allowance, Benefit, Statutory).</param>
    /// <param name="calculationMethod">How the value is computed.</param>
    /// <param name="calculationValue">Amount or percentage; required for Fixed and Percentage methods.</param>
    /// <param name="formula">Expression string; required for Formula method.</param>
    /// <param name="isTaxable">Whether this component is included in taxable income.</param>
    /// <param name="isFnpfApplicable">Whether this component is included in FNPF-applicable gross.</param>
    /// <param name="displayOrder">Sort order on payslips (non-negative).</param>
    /// <param name="description">Optional description.</param>
    /// <param name="isSystemComponent">Reserved for seed data; system components cannot be modified.</param>
    /// <returns>A fully validated <see cref="PayrollComponent"/>.</returns>
    /// <exception cref="DomainException">Thrown when business rules are violated.</exception>
    public static PayrollComponent Create(
        int companyId,
        string componentCode,
        string componentName,
        ComponentType componentType,
        CalculationMethod calculationMethod,
        decimal? calculationValue,
        string? formula,
        bool isTaxable,
        bool isFnpfApplicable,
        int displayOrder,
        string? description = null,
        bool isSystemComponent = false)
    {
        Guard.AgainstCondition(companyId <= 0, "CompanyId must be a positive integer.");

        var component = new PayrollComponent
        {
            CompanyId         = companyId,
            ComponentCode     = Guard.AgainstMaxLength(componentCode.Trim().ToUpperInvariant(), 20, nameof(componentCode)),
            ComponentName     = Guard.AgainstMaxLength(componentName.Trim(), 200, nameof(componentName)),
            ComponentType     = Guard.AgainstInvalidEnum(componentType),
            CalculationMethod = Guard.AgainstInvalidEnum(calculationMethod),
            IsTaxable         = isTaxable,
            IsFnpfApplicable  = isFnpfApplicable,
            DisplayOrder      = displayOrder,
            Description       = description?.Trim(),
            IsSystemComponent = isSystemComponent,
            IsActive          = true,
        };

        component.SetCalculationValues(calculationMethod, calculationValue, formula);

        return component;
    }

    // ─── Domain Methods ──────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the component's name, calculation method, tax flags, and display order.
    /// System components have restricted editing — only name and display order can change.
    /// </summary>
    /// <param name="componentName">Updated display name.</param>
    /// <param name="componentType">Updated type (restricted for system components).</param>
    /// <param name="calculationMethod">Updated calculation method (restricted for system components).</param>
    /// <param name="calculationValue">Updated value or percentage.</param>
    /// <param name="formula">Updated formula expression.</param>
    /// <param name="isTaxable">Updated taxability flag.</param>
    /// <param name="isFnpfApplicable">Updated FNPF applicability flag.</param>
    /// <param name="displayOrder">Updated display order.</param>
    /// <param name="description">Updated description.</param>
    /// <exception cref="DomainException">Thrown when a system component's restricted fields are changed.</exception>
    public void Update(
        string componentName,
        ComponentType componentType,
        CalculationMethod calculationMethod,
        decimal? calculationValue,
        string? formula,
        bool isTaxable,
        bool isFnpfApplicable,
        int displayOrder,
        string? description)
    {
        if (IsSystemComponent)
        {
            // System components: only name and display order are editable
            ComponentName = Guard.AgainstMaxLength(componentName.Trim(), 200, nameof(componentName));
            DisplayOrder  = displayOrder;
            Description   = description?.Trim();
            return;
        }

        ComponentName     = Guard.AgainstMaxLength(componentName.Trim(), 200, nameof(componentName));
        ComponentType     = Guard.AgainstInvalidEnum(componentType);
        CalculationMethod = Guard.AgainstInvalidEnum(calculationMethod);
        IsTaxable         = isTaxable;
        IsFnpfApplicable  = isFnpfApplicable;
        DisplayOrder      = displayOrder;
        Description       = description?.Trim();

        SetCalculationValues(calculationMethod, calculationValue, formula);
    }

    /// <summary>
    /// Activates this component so it is included in payroll calculations.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates this component so it is excluded from future payroll calculations.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown if this is a system component — system components cannot be deactivated.
    /// </exception>
    public void Deactivate()
    {
        if (IsSystemComponent)
        {
            throw new DomainException(
                $"System component '{ComponentCode}' cannot be deactivated. " +
                "System components are mandatory for payroll calculation.");
        }

        IsActive = false;
    }

    /// <summary>
    /// Logically deletes this component. Prevents use in future payroll runs.
    /// </summary>
    /// <param name="deletedBy">Username performing the deletion.</param>
    /// <exception cref="DomainException">Thrown if this is a system component.</exception>
    public new void SoftDelete(string deletedBy)
    {
        if (IsSystemComponent)
        {
            throw new DomainException(
                $"System component '{ComponentCode}' cannot be deleted. " +
                "System components are mandatory for payroll calculation.");
        }

        base.SoftDelete(deletedBy);
        IsActive = false;
    }

    /// <summary>
    /// Creates a deep copy of this component with a new code, name, and no system flag.
    /// Used for the "Duplicate" feature in the UI.
    /// </summary>
    /// <param name="newCode">The code for the duplicated component.</param>
    /// <param name="newName">The name for the duplicated component.</param>
    /// <returns>A new unsaved <see cref="PayrollComponent"/> with the same settings.</returns>
    public PayrollComponent Duplicate(string newCode, string newName)
    {
        return Create(
            companyId:         CompanyId,
            componentCode:     newCode,
            componentName:     newName,
            componentType:     ComponentType,
            calculationMethod: CalculationMethod,
            calculationValue:  CalculationValue,
            formula:           Formula,
            isTaxable:         IsTaxable,
            isFnpfApplicable:  IsFnpfApplicable,
            displayOrder:      DisplayOrder,
            description:       Description,
            isSystemComponent: false);
    }

    /// <summary>
    /// Evaluates whether this component has a valid configuration for the
    /// given calculation method. Returns a list of validation error messages.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (CalculationMethod is CalculationMethod.Fixed or CalculationMethod.Percentage)
        {
            if (!CalculationValue.HasValue || CalculationValue <= 0m)
            {
                errors.Add($"A {CalculationMethod} component requires a positive CalculationValue.");
            }
        }

        if (CalculationMethod == CalculationMethod.Formula)
        {
            if (string.IsNullOrWhiteSpace(Formula))
            {
                errors.Add("A Formula component requires a non-empty Formula expression.");
            }
        }

        if (ComponentCode.Length > 20)
        {
            errors.Add("ComponentCode must not exceed 20 characters.");
        }

        if (ComponentName.Length > 200)
        {
            errors.Add("ComponentName must not exceed 200 characters.");
        }

        return errors.AsReadOnly();
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sets and cross-validates CalculationValue and Formula based on the chosen method.
    /// </summary>
    private void SetCalculationValues(
        CalculationMethod method,
        decimal? calculationValue,
        string? formula)
    {
        switch (method)
        {
            case CalculationMethod.Fixed:
            case CalculationMethod.Percentage:
                if (!calculationValue.HasValue || calculationValue <= 0m)
                {
                    throw new DomainException(
                        $"CalculationValue must be a positive number for a {method} component.");
                }

                CalculationValue = calculationValue;
                Formula = null;
                break;

            case CalculationMethod.Formula:
                if (string.IsNullOrWhiteSpace(formula))
                {
                    throw new DomainException(
                        "Formula expression must not be empty for a Formula component.");
                }

                Formula = formula.Trim();
                CalculationValue = null;
                break;

            case CalculationMethod.Manual:
                CalculationValue = null;
                Formula = null;
                break;

            default:
                throw new DomainException($"Unknown CalculationMethod '{method}'.");
        }
    }
}
