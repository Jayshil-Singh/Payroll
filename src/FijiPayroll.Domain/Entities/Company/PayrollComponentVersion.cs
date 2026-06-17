using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a historical, immutable snapshot version of a payroll component rule/state.
/// </summary>
public sealed class PayrollComponentVersion : BaseEntity
{
    private PayrollComponentVersion() { }

    public PayrollComponentVersion(
        int payrollComponentId,
        int versionNumber,
        string versionHash,
        string expressionText,
        string calculationMethod,
        bool taxable,
        bool subjectToFnpf,
        bool recurring,
        int priority,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string createdBy,
        DateTime createdDate,
        int? createdFromPayrollRunId = null)
    {
        PayrollComponentId = payrollComponentId;
        VersionNumber = versionNumber;
        VersionHash = versionHash;
        ExpressionText = expressionText;
        CalculationMethod = calculationMethod;
        Taxable = taxable;
        SubjectToFNPF = subjectToFnpf;
        Recurring = recurring;
        Priority = priority;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CreatedBy = createdBy;
        CreatedDate = createdDate;
        CreatedFromPayrollRunId = createdFromPayrollRunId;
    }

    public int PayrollComponentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string VersionHash { get; private set; } = string.Empty;
    public string ExpressionText { get; private set; } = string.Empty;
    public string CalculationMethod { get; private set; } = string.Empty;
    public bool Taxable { get; private set; }
    public bool SubjectToFNPF { get; private set; }
    public bool Recurring { get; private set; }
    public int Priority { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedDate { get; private set; }
    public int? CreatedFromPayrollRunId { get; private set; }

    // Navigation
    public PayrollComponent? PayrollComponent { get; private set; }
}
