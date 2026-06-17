using System;

namespace FijiPayroll.SDK.Contracts;

/// <summary>
/// Immutable version contract representing pinned versions of different calculation engines and legislative rules.
/// Ensures that historical payroll cycles are reproducible.
/// </summary>
public sealed record VersionContract(
    string CalculationEngineVersion,
    string FormulaEngineVersion,
    string ComplianceEngineVersion,
    string RuleVersion
);
