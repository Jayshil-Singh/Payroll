using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Execution engine for running dry-run simulations of statutory rule changes over finalized payroll ledgers.
/// </summary>
public sealed class RuleSimulationEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RuleSimulationEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSimulationEngine"/> class.
    /// </summary>
    public RuleSimulationEngine(IUnitOfWork unitOfWork, ILogger<RuleSimulationEngine> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Represents a rule override parameter.
    /// </summary>
    public sealed record RuleOverride(string RuleCode, string NewValue);

    /// <summary>
    /// Represents a summary variance report for the entire simulation run.
    /// </summary>
    public sealed class RuleSimulationResult
    {
        public decimal OriginalTotalFnpf { get; set; }
        public decimal SimulatedTotalFnpf { get; set; }
        public decimal OriginalTotalPaye { get; set; }
        public decimal SimulatedTotalPaye { get; set; }
        public List<EmployeeVariance> EmployeeVariances { get; set; } = new();

        public decimal FnpfVariance => SimulatedTotalFnpf - OriginalTotalFnpf;
        public decimal PayeVariance => SimulatedTotalPaye - OriginalTotalPaye;
    }

    /// <summary>
    /// Represents variance details for an individual employee.
    /// </summary>
    public sealed class EmployeeVariance
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal OriginalFnpf { get; set; }
        public decimal SimulatedFnpf { get; set; }
        public decimal OriginalPaye { get; set; }
        public decimal SimulatedPaye { get; set; }

        public decimal FnpfDifference => SimulatedFnpf - OriginalFnpf;
        public decimal PayeDifference => SimulatedPaye - OriginalPaye;
    }

    /// <summary>
    /// Runs a compliance calculation simulation over the payroll ledger items for a pay run, applying rule overrides in-memory.
    /// </summary>
    public async Task<RuleSimulationResult> SimulateRuleChangeAsync(
        int companyId,
        int payrollRunId,
        List<RuleOverride> overrides,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting rule simulation for Run {RunId} under Company {CompanyId}", payrollRunId, companyId);

        var result = new RuleSimulationResult();

        // 1. Fetch ledgers
        var ledgers = await _unitOfWork.Compliance.GetLedgerByRunIdAsync(payrollRunId, cancellationToken);
        if (!ledgers.Any())
        {
            _logger.LogWarning("No finalized ledger records found for run {RunId} to run simulation.", payrollRunId);
            return result;
        }

        // 2. Load baseline statutory rules
        var effectiveDate = DateTime.UtcNow;
        var ruleOverridesDict = overrides.ToDictionary(x => x.RuleCode, x => x.NewValue, StringComparer.OrdinalIgnoreCase);

        // Resolve rates
        decimal fnpfEmployeeRate = await GetRuleDecimalAsync("FNPF", "FNPF_EE_RATE", 0.08m, ruleOverridesDict, effectiveDate, cancellationToken);
        decimal fnpfEmployerRate = await GetRuleDecimalAsync("FNPF", "FNPF_ER_RATE", 0.10m, ruleOverridesDict, effectiveDate, cancellationToken);
        decimal taxFreeThreshold = await GetRuleDecimalAsync("FRCS", "PAYE_TAX_FREE_THRESHOLD", 30000m, ruleOverridesDict, effectiveDate, cancellationToken);
        decimal payeRate1 = await GetRuleDecimalAsync("FRCS", "PAYE_BRACKET_1_RATE", 0.18m, ruleOverridesDict, effectiveDate, cancellationToken);
        decimal payeRate2 = await GetRuleDecimalAsync("FRCS", "PAYE_BRACKET_2_RATE", 0.20m, ruleOverridesDict, effectiveDate, cancellationToken);

        foreach (var ledger in ledgers)
        {
            // Calculate simulated FNPF
            decimal simFnpfEmployee = Math.Round(ledger.Gross * fnpfEmployeeRate, 2);
            decimal simFnpfEmployer = Math.Round(ledger.Gross * fnpfEmployerRate, 2);
            decimal simTotalFnpf = simFnpfEmployee + simFnpfEmployer;
            decimal origTotalFnpf = ledger.FNPFEmployee + ledger.FNPFEmployer;

            // Calculate simulated PAYE (simplified brackets matching seeds)
            decimal annualGross = ledger.Gross * 12; // annualized gross
            decimal taxableIncome = Math.Max(0, annualGross - taxFreeThreshold);
            decimal annualPaye = 0;
            if (taxableIncome > 0)
            {
                if (taxableIncome <= 20000)
                {
                    annualPaye = taxableIncome * payeRate1;
                }
                else
                {
                    annualPaye = (20000 * payeRate1) + ((taxableIncome - 20000) * payeRate2);
                }
            }
            decimal simPaye = Math.Round(annualPaye / 12, 2);

            result.OriginalTotalFnpf += origTotalFnpf;
            result.SimulatedTotalFnpf += simTotalFnpf;
            result.OriginalTotalPaye += ledger.PAYE;
            result.SimulatedTotalPaye += simPaye;

            result.EmployeeVariances.Add(new EmployeeVariance
            {
                EmployeeId = ledger.EmployeeId,
                EmployeeName = ledger.EmployeeName,
                OriginalFnpf = origTotalFnpf,
                SimulatedFnpf = simTotalFnpf,
                OriginalPaye = ledger.PAYE,
                SimulatedPaye = simPaye
            });
        }

        _logger.LogInformation("Rule simulation complete. FNPF Variance: {FnpfVar:N2}, PAYE Variance: {PayeVar:N2}", result.FnpfVariance, result.PayeVariance);
        return result;
    }

    private async Task<decimal> GetRuleDecimalAsync(
        string authority,
        string ruleCode,
        decimal defaultFallback,
        Dictionary<string, string> overrides,
        DateTime date,
        CancellationToken cancellationToken)
    {
        if (overrides.TryGetValue(ruleCode, out string? valueStr) && decimal.TryParse(valueStr, out decimal ovDec))
        {
            return ovDec;
        }

        var rule = await _unitOfWork.Compliance.GetStatutoryRuleAsync(authority, ruleCode, date, cancellationToken);
        if (rule != null && decimal.TryParse(rule.RuleValue, out decimal ruleDec))
        {
            return ruleDec;
        }

        return defaultFallback;
    }
}
