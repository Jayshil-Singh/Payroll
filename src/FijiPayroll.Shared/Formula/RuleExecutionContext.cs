using System.Collections.Generic;
using System.Threading;

namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Immutable execution context mapping variables and tenant boundaries for a rule calculation.
/// </summary>
public sealed class RuleExecutionContext
{
    /// <summary>
    /// Initialises a new instance of the <see cref="RuleExecutionContext"/> class.
    /// </summary>
    public RuleExecutionContext(
        int companyId,
        int? branchId,
        int? departmentId,
        int? employeeId,
        int? payrollRunId,
        int? ruleSetId,
        int fiscalYear,
        string payrollFrequency,
        string currency,
        string culture,
        IReadOnlyDictionary<string, decimal> variables,
        CancellationToken cancellationToken = default)
    {
        CompanyId = companyId;
        BranchId = branchId;
        DepartmentId = departmentId;
        EmployeeId = employeeId;
        PayrollRunId = payrollRunId;
        RuleSetId = ruleSetId;
        FiscalYear = fiscalYear;
        PayrollFrequency = payrollFrequency;
        Currency = currency;
        Culture = culture;
        Variables = variables;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; }

    /// <summary>Gets the branch ID.</summary>
    public int? BranchId { get; }

    /// <summary>Gets the department ID.</summary>
    public int? DepartmentId { get; }

    /// <summary>Gets the employee ID.</summary>
    public int? EmployeeId { get; }

    /// <summary>Gets the payroll run ID.</summary>
    public int? PayrollRunId { get; }

    /// <summary>Gets the rule set ID.</summary>
    public int? RuleSetId { get; }

    /// <summary>Gets the fiscal year.</summary>
    public int FiscalYear { get; }

    /// <summary>Gets the payroll frequency (e.g. Weekly, Fortnightly, Monthly).</summary>
    public string PayrollFrequency { get; }

    /// <summary>Gets the currency code (e.g. FJD).</summary>
    public string Currency { get; }

    /// <summary>Gets the culture code (e.g. en-FJ).</summary>
    public string Culture { get; }

    /// <summary>Gets the read-only dictionary of variables available to the engine.</summary>
    public IReadOnlyDictionary<string, decimal> Variables { get; }

    /// <summary>Gets the cancellation token for the rule execution.</summary>
    public CancellationToken CancellationToken { get; }
}
