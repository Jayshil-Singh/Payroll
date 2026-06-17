using System.Collections.Generic;

namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Contains the output and execution metrics/trace from a business rule execution.
/// </summary>
public sealed class RuleExecutionResult
{
    /// <summary>
    /// Initialises a new instance of the <see cref="RuleExecutionResult"/> class.
    /// </summary>
    public RuleExecutionResult(
        decimal value,
        IReadOnlyList<string> variablesUsed,
        IReadOnlyList<string> rulesExecuted,
        double executionTime,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors,
        string executionHash,
        int ruleVersion,
        string executionTrace)
    {
        Value = value;
        VariablesUsed = variablesUsed;
        RulesExecuted = rulesExecuted;
        ExecutionTime = executionTime;
        Warnings = warnings;
        Errors = errors;
        ExecutionHash = executionHash;
        RuleVersion = ruleVersion;
        ExecutionTrace = executionTrace;
    }

    /// <summary>Gets the calculated value.</summary>
    public decimal Value { get; }

    /// <summary>Gets the list of variables referenced during evaluation.</summary>
    public IReadOnlyList<string> VariablesUsed { get; }

    /// <summary>Gets the list of rule codes/IDs evaluated.</summary>
    public IReadOnlyList<string> RulesExecuted { get; }

    /// <summary>Gets the time taken in milliseconds to evaluate the rule.</summary>
    public double ExecutionTime { get; }

    /// <summary>Gets any warning messages produced during compilation or execution.</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Gets any error messages produced during compilation or execution.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Gets the SHA256 audit hash of the expression.</summary>
    public string ExecutionHash { get; }

    /// <summary>Gets the version of the rule executed.</summary>
    public int RuleVersion { get; }

    /// <summary>Gets a detailed trace log of the calculation steps.</summary>
    public string ExecutionTrace { get; }
}
