using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a module context that can use and evaluate rules (e.g. Payroll, Leave, Overtime).
/// </summary>
public sealed class RuleModule : BaseEntity
{
    private RuleModule() { }

    public RuleModule(string code, string name, string description, int executionPriority, bool isSystem)
    {
        Code = code;
        Name = name;
        Description = description;
        ExecutionPriority = executionPriority;
        IsSystem = isSystem;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int ExecutionPriority { get; private set; }
    public bool IsSystem { get; private set; }
}
