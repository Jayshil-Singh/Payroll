using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a dynamic, authority-based statutory rule (e.g. FNPF rates or PAYE brackets).
/// Helps avoid recompilation when legislation parameters change.
/// </summary>
public sealed class StatutoryRule : AuditableEntity
{
    /// <summary>Gets the statutory authority governing this rule (e.g., "FRCS", "FNPF").</summary>
    public string Authority { get; private set; } = string.Empty;

    /// <summary>Gets the unique code key identifying this configuration parameter (e.g., "FNPF_EE_RATE").</summary>
    public string RuleCode { get; private set; } = string.Empty;

    /// <summary>Gets the configuration value (could represent raw values, percentages, or serialized range structures).</summary>
    public string RuleValue { get; private set; } = string.Empty;

    /// <summary>Gets the description explaining what this rule configures.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the timestamp when this rule configuration becomes active.</summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>Gets the timestamp when this rule configuration ceases to be active (optional).</summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>Gets a value indicating whether this rule is currently active.</summary>
    public bool IsActive { get; private set; }

    private StatutoryRule() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new StatutoryRule.
    /// </summary>
    public static StatutoryRule Create(
        string authority,
        string ruleCode,
        string ruleValue,
        string description,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null)
    {
        if (string.IsNullOrWhiteSpace(authority)) throw new ArgumentException("Authority cannot be empty.", nameof(authority));
        if (string.IsNullOrWhiteSpace(ruleCode)) throw new ArgumentException("Rule code cannot be empty.", nameof(ruleCode));
        if (string.IsNullOrWhiteSpace(ruleValue)) throw new ArgumentException("Rule value cannot be empty.", nameof(ruleValue));
        if (effectiveTo.HasValue && effectiveFrom >= effectiveTo.Value) throw new ArgumentException("Effective from date must be before effective to date.");

        return new StatutoryRule
        {
            Authority = authority,
            RuleCode = ruleCode,
            RuleValue = ruleValue,
            Description = description,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        };
    }

    /// <summary>Deactivates the statutory rule.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Activates the statutory rule.</summary>
    public void Activate() => IsActive = true;
}
