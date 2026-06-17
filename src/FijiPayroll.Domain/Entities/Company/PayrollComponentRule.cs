using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a rule assigned to a payroll component.
/// </summary>
public sealed class PayrollComponentRule : AuditableEntity
{
    private PayrollComponentRule() { }

    public PayrollComponentRule(
        int componentId,
        int ruleModuleId,
        string ruleType,
        string expressionText,
        string compiledHash,
        int compiledVersion,
        int priority,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        int ruleVersion)
    {
        ComponentId = componentId;
        RuleModuleId = ruleModuleId;
        RuleType = ruleType;
        ExpressionText = expressionText;
        CompiledHash = compiledHash;
        CompiledVersion = compiledVersion;
        Priority = priority;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        RuleVersion = ruleVersion;
    }

    public int ComponentId { get; private set; }
    public int RuleModuleId { get; private set; }
    public string RuleType { get; private set; } = string.Empty;
    public string ExpressionText { get; private set; } = string.Empty;
    public string CompiledHash { get; private set; } = string.Empty;
    public int CompiledVersion { get; private set; }
    public int Priority { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public int RuleVersion { get; private set; }

    // Navigation properties
    public PayrollComponent? Component { get; private set; }
    public RuleModule? RuleModule { get; private set; }
}
