using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a set of rules that governs a specific module calculations for a company.
/// Supports inheritance hierarchies.
/// </summary>
public sealed class RuleSet : AuditableEntity
{
    private RuleSet() { }

    public RuleSet(
        string name,
        string description,
        string version,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        ComponentStatus status,
        int companyId,
        int? parentRuleSetId = null,
        bool isSystem = false,
        bool isLocked = false)
    {
        Name = name;
        Description = description;
        Version = version;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Status = status;
        CompanyId = companyId;
        ParentRuleSetId = parentRuleSetId;
        IsSystem = isSystem;
        IsLocked = isLocked;
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public ComponentStatus Status { get; private set; }
    public int CompanyId { get; private set; }
    public int? ParentRuleSetId { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsLocked { get; private set; }

    // Navigation properties
    public RuleSet? ParentRuleSet { get; private set; }
    public ICollection<RuleSet> ChildRuleSets { get; private set; } = new List<RuleSet>();
}
