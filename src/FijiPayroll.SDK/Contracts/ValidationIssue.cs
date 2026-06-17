using System;

namespace FijiPayroll.SDK.Contracts;

/// <summary>
/// Represents a validation warning or error produced during compliance auditing operations.
/// </summary>
public sealed record ValidationIssue(
    string Severity, // "Info", "Warning", "Error"
    string Message,
    string AffectedEmployee,
    string RuleCode,
    string RecommendedAction
);
