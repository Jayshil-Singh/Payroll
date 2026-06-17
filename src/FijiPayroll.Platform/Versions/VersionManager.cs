using System;
using System.Collections.Concurrent;
using FijiPayroll.SDK.Contracts;

namespace FijiPayroll.Platform.Versions;

/// <summary>
/// Manages system-wide engine versions and allows pinning versions for individual payroll runs to guarantee reproducibility.
/// </summary>
public sealed class VersionManager
{
    private readonly ConcurrentDictionary<int, VersionContract> _pinnedRunVersions = new();

    /// <summary>
    /// Gets the current baseline versions of the running platform engines.
    /// </summary>
    public VersionContract GetCurrentPlatformVersions()
    {
        return new VersionContract(
            CalculationEngineVersion: "Payroll Engine 1.0.0",
            FormulaEngineVersion: "Formula Engine 2.1.0",
            ComplianceEngineVersion: "Compliance Engine 1.3.0",
            RuleVersion: "Statutory Rules v2026.01"
        );
    }

    /// <summary>
    /// Pins the engine versions for a specific payroll run.
    /// </summary>
    /// <param name="payrollRunId">The payroll run identifier.</param>
    /// <param name="versions">The version set to pin.</param>
    public void PinVersionsForRun(int payrollRunId, VersionContract versions)
    {
        if (versions == null) throw new ArgumentNullException(nameof(versions));
        _pinnedRunVersions[payrollRunId] = versions;
    }

    /// <summary>
    /// Gets the pinned versions for a specific payroll run, fallback to current platform versions if not pinned.
    /// </summary>
    /// <param name="payrollRunId">The payroll run identifier.</param>
    /// <returns>The pinned version details.</returns>
    public VersionContract GetVersionsForRun(int payrollRunId)
    {
        if (_pinnedRunVersions.TryGetValue(payrollRunId, out var pinned))
        {
            return pinned;
        }
        return GetCurrentPlatformVersions();
    }
}
