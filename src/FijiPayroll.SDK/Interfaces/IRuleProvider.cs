using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Defines the contract for fetching dynamic statutory rules and rates (e.g. FNPF rates, PAYE brackets).
/// </summary>
public interface IRuleProvider
{
    /// <summary>
    /// Retrieves a statutory rule value as a string.
    /// </summary>
    /// <param name="authority">The statutory authority (e.g. "FRCS", "FNPF").</param>
    /// <param name="ruleCode">The unique rule identifier code.</param>
    /// <param name="date">The effective date for checking the rule value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dynamic rule value as a string.</returns>
    Task<string> GetRuleValueAsync(
        string authority,
        string ruleCode,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a statutory rule value parsed as a decimal.
    /// </summary>
    /// <param name="authority">The statutory authority (e.g. "FRCS", "FNPF").</param>
    /// <param name="ruleCode">The unique rule identifier code.</param>
    /// <param name="date">The effective date for checking the rule value.</param>
    /// <param name="defaultValue">The fallback value if the rule is not found or parsing fails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dynamic rule value parsed as a decimal, or the default value.</returns>
    Task<decimal> GetDecimalRuleValueAsync(
        string authority,
        string ruleCode,
        DateTime date,
        decimal defaultValue = 0,
        CancellationToken cancellationToken = default);
}
