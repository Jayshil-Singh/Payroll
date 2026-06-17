using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a dependency between two payroll components.
/// </summary>
public sealed class PayrollComponentDependency : BaseEntity
{
    private PayrollComponentDependency() { }

    public PayrollComponentDependency(
        int parentComponentId,
        int childComponentId,
        string dependencyType,
        int calculationOrder,
        bool required)
    {
        ParentComponentId = parentComponentId;
        ChildComponentId = childComponentId;
        DependencyType = dependencyType;
        CalculationOrder = calculationOrder;
        Required = required;
    }

    public int ParentComponentId { get; private set; }
    public int ChildComponentId { get; private set; }
    public string DependencyType { get; private set; } = string.Empty;
    public int CalculationOrder { get; private set; }
    public bool Required { get; private set; }

    // Navigation properties
    public PayrollComponent? ParentComponent { get; private set; }
    public PayrollComponent? ChildComponent { get; private set; }
}
