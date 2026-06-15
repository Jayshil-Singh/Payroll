using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Detailed snapshot line item representing a component calculation (earnings, deductions, allowances)
/// for a specific employee within a payroll run.
/// </summary>
public sealed class PayrollRunLineItem : BaseEntity
{
    private string _componentCode = string.Empty;
    private string _componentName = string.Empty;

    private PayrollRunLineItem() { }

    /// <summary>
    /// Foreign key to PayrollRunEmployee.
    /// </summary>
    public int PayrollRunEmployeeId { get; private set; }

    /// <summary>
    /// The specific database component ID referenced at calculation time.
    /// </summary>
    public int ComponentId { get; private set; }

    /// <summary>
    /// Code of the component (e.g. "BASIC", "PAYE", "FNPF-EMP").
    /// </summary>
    public string ComponentCode
    {
        get => _componentCode;
        private set => _componentCode = Guard.AgainstNullOrWhiteSpace(value).ToUpperInvariant();
    }

    /// <summary>
    /// Display name of the component.
    /// </summary>
    public string ComponentName
    {
        get => _componentName;
        private set => _componentName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Component category.
    /// </summary>
    public ComponentType ComponentType { get; private set; }

    /// <summary>
    /// Calculated dollar amount.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Flag indicating whether the component is taxable.
    /// </summary>
    public bool IsTaxable { get; private set; }

    /// <summary>
    /// Flag indicating if the component is subject to FNPF.
    /// </summary>
    public bool AffectsFnpf { get; private set; }

    /// <summary>
    /// Flag indicating if this represents an employer contribution.
    /// </summary>
    public bool EmployerContributionFlag { get; private set; }

    /// <summary>
    /// Snapshot reference back to the original source component ID.
    /// </summary>
    public int ReferenceComponentId { get; private set; }

    /// <summary>
    /// Factory method to create a line item.
    /// </summary>
    public static PayrollRunLineItem Create(
        int payrollRunEmployeeId,
        int componentId,
        string componentCode,
        string componentName,
        ComponentType componentType,
        decimal amount,
        bool isTaxable,
        bool affectsFnpf,
        bool employerContributionFlag,
        int referenceComponentId)
    {
        return new PayrollRunLineItem
        {
            PayrollRunEmployeeId = payrollRunEmployeeId,
            ComponentId = componentId,
            ComponentCode = componentCode,
            ComponentName = componentName,
            ComponentType = Guard.AgainstInvalidEnum(componentType),
            Amount = amount,
            IsTaxable = isTaxable,
            AffectsFnpf = affectsFnpf,
            EmployerContributionFlag = employerContributionFlag,
            ReferenceComponentId = referenceComponentId
        };
    }
}
