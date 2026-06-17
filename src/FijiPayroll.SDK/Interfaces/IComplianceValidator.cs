using System.Collections.Generic;
using FijiPayroll.SDK.Contracts;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Interface for legislative or bank rules validation engines.
/// </summary>
public interface IComplianceValidator
{
    /// <summary>Gets the unique registry rule identifier code.</summary>
    string RuleCode { get; }

    /// <summary>Gets the display name of the validation check.</summary>
    string RuleName { get; }

    /// <summary>
    /// Evaluates the target context payload and returns a list of validation issues.
    /// </summary>
    /// <param name="companyId">The company identifier context.</param>
    /// <param name="payload">The target model or dataset to validate.</param>
    /// <returns>Sequence of validation warnings or errors.</returns>
    IEnumerable<ValidationIssue> Validate(int companyId, object payload);
}
