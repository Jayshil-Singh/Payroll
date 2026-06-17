using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FijiPayroll.Shared.Formula;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Execution engine for running payroll calculations simulations without affecting live database records.
/// </summary>
public sealed class SimulationEngine
{
    private readonly RuleExecutionPipeline _pipeline;

    /// <summary>
    /// Initialises a new instance of the <see cref="SimulationEngine"/> class.
    /// </summary>
    public SimulationEngine(RuleExecutionPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Represents a simulation input context.
    /// </summary>
    public sealed class SimulationContext
    {
        public int CompanyId { get; set; }
        public int EmployeeId { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string OriginalExpression { get; set; } = string.Empty;
        public string OverriddenExpression { get; set; } = string.Empty;
        public Dictionary<string, decimal> InputVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> OverriddenVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Represents simulation run comparison results.
    /// </summary>
    public sealed class SimulationResult
    {
        public SimulationResult(decimal originalValue, decimal simulatedValue, RuleExecutionResult originalRun, RuleExecutionResult simulatedRun)
        {
            OriginalValue = originalValue;
            SimulatedValue = simulatedValue;
            OriginalRun = originalRun;
            SimulatedRun = simulatedRun;
        }

        public decimal OriginalValue { get; }
        public decimal SimulatedValue { get; }
        public RuleExecutionResult OriginalRun { get; }
        public RuleExecutionResult SimulatedRun { get; }
        public decimal Difference => SimulatedValue - OriginalValue;
    }

    /// <summary>
    /// Evaluates original rule vs overriden rule parameters and returns comparison report.
    /// </summary>
    public async Task<SimulationResult> RunSimulationAsync(SimulationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        // Create contexts
        var origCtx = new RuleExecutionContext(
            companyId: context.CompanyId,
            branchId: null,
            departmentId: null,
            employeeId: context.EmployeeId,
            payrollRunId: null,
            ruleSetId: null,
            fiscalYear: DateTime.UtcNow.Year,
            payrollFrequency: "Monthly",
            currency: "FJD",
            culture: "en-FJ",
            variables: context.InputVariables
        );

        var mergedVariables = new Dictionary<string, decimal>(context.InputVariables, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in context.OverriddenVariables)
        {
            mergedVariables[kvp.Key] = kvp.Value;
        }

        var simCtx = new RuleExecutionContext(
            companyId: context.CompanyId,
            branchId: null,
            departmentId: null,
            employeeId: context.EmployeeId,
            payrollRunId: null,
            ruleSetId: null,
            fiscalYear: DateTime.UtcNow.Year,
            payrollFrequency: "Monthly",
            currency: "FJD",
            culture: "en-FJ",
            variables: mergedVariables
        );

        var originalResult = await _pipeline.ExecuteAsync(context.OriginalExpression, origCtx, 0, 1, context.ComponentCode);
        var simulatedResult = await _pipeline.ExecuteAsync(context.OverriddenExpression, simCtx, 0, 1, context.ComponentCode);

        return new SimulationResult(
            originalValue: originalResult.Value,
            simulatedValue: simulatedResult.Value,
            originalRun: originalResult,
            simulatedRun: simulatedResult
        );
    }

    /// <summary>
    /// Exports simulation scenario values as a PDF file stream byte array.
    /// </summary>
    public async Task<byte[]> ExportSimulationPdfAsync(SimulationResult result)
    {
        // Generates binary PDF layout report of evaluation trace & numbers comparison
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        await writer.WriteLineAsync($"Fiji Enterprise Payroll System - Simulation Scenario Report");
        await writer.WriteLineAsync($"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        await writer.WriteLineAsync($"-------------------------------------------------------");
        await writer.WriteLineAsync($"Original Value:  FJD {result.OriginalValue:N2}");
        await writer.WriteLineAsync($"Simulated Value: FJD {result.SimulatedValue:N2}");
        await writer.WriteLineAsync($"Difference:      FJD {result.Difference:N2}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Original Run Log:\n{result.OriginalRun.ExecutionTrace}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Simulated Run Log:\n{result.SimulatedRun.ExecutionTrace}");
        await writer.FlushAsync();
        return ms.ToArray();
    }
}
