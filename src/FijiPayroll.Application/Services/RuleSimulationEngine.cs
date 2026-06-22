using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Domain.Rules.PayrollRules;
using FijiPayroll.Shared.Constants;
using FijiPayroll.Shared.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Compliance simulation engine. Uses the same PAYE/FNPF domain engines as live payroll calculation.
/// </summary>
public sealed class RuleSimulationEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RuleSimulationEngine> _logger;

    public RuleSimulationEngine(IUnitOfWork unitOfWork, ILogger<RuleSimulationEngine> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public sealed record RuleOverride(string RuleCode, string NewValue);

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

    public async Task<RuleSimulationResult> SimulateRuleChangeAsync(
        int companyId,
        int payrollRunId,
        List<RuleOverride> overrides,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting rule simulation for Run {RunId} under Company {CompanyId}", payrollRunId, companyId);

        var result = new RuleSimulationResult();
        var ledgers = await _unitOfWork.Compliance.GetLedgerByRunIdAsync(payrollRunId, cancellationToken);

        if (!ledgers.Any())
        {
            _logger.LogWarning("No finalized ledger records found for run {RunId} to run simulation.", payrollRunId);
            return result;
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(payrollRunId, cancellationToken);
        if (run == null || run.CompanyId != companyId)
        {
            throw new InvalidOperationException($"Payroll run {payrollRunId} not found for company {companyId}.");
        }

        var residencyByEmployee = run.Employees
            .Where(e => !e.IsSuperseded)
            .GroupBy(e => e.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().ResidencyStatus);

        var taxBrackets = await _unitOfWork.TaxBrackets.GetBracketsByVersionAndFrequencyAsync(
            FijiTaxConstants.CurrentTaxVersion,
            run.Frequency,
            cancellationToken);

        var ruleOverridesDict = overrides.ToDictionary(x => x.RuleCode, x => x.NewValue, StringComparer.OrdinalIgnoreCase);
        var effectiveDate = DateTime.UtcNow;

        decimal fnpfEmployeeRate = await ResolveRateAsync(
            "FNPF_EE_RATE", FijiTaxConstants.DefaultFnpfEmployeeRate, ruleOverridesDict, effectiveDate, cancellationToken);
        decimal fnpfEmployerRate = await ResolveRateAsync(
            "FNPF_ER_RATE", FijiTaxConstants.DefaultFnpfEmployerRate, ruleOverridesDict, effectiveDate, cancellationToken);

        var fnpfConfig = await _unitOfWork.Setup.GetActiveFnpfConfigurationAsync(companyId, cancellationToken);
        if (fnpfConfig != null && !ruleOverridesDict.ContainsKey("FNPF_EE_RATE"))
        {
            fnpfEmployeeRate = fnpfConfig.EmployeeRate;
        }

        if (fnpfConfig != null && !ruleOverridesDict.ContainsKey("FNPF_ER_RATE"))
        {
            fnpfEmployerRate = fnpfConfig.EmployerRate;
        }

        foreach (var ledger in ledgers)
        {
            decimal origTotalFnpf = (ledger.FNPFEmployee + ledger.FNPFEmployer).ToFijiRound();

            decimal simFnpfEmployee = PayrollDeductionEngine.CalculateEmployeeFnpf(
                ledger.Gross, false, fnpfEmployeeRate).ToFijiRound();
            decimal simFnpfEmployer = PayrollDeductionEngine.CalculateEmployerFnpf(
                ledger.Gross, false, fnpfEmployerRate).ToFijiRound();
            decimal simTotalFnpf = (simFnpfEmployee + simFnpfEmployer).ToFijiRound();

            decimal taxableGross = ledger.Gross;
            string residency = residencyByEmployee.TryGetValue(ledger.EmployeeId, out var rs)
                ? rs
                : "Resident";

            decimal simPaye = PayrollTaxEngine.CalculatePaye(
                taxableGross,
                simFnpfEmployee,
                run.Frequency,
                residency,
                FijiTaxConstants.CurrentTaxVersion,
                taxBrackets).ToFijiRound();

            result.OriginalTotalFnpf += origTotalFnpf;
            result.SimulatedTotalFnpf += simTotalFnpf;
            result.OriginalTotalPaye += ledger.PAYE.ToFijiRound();
            result.SimulatedTotalPaye += simPaye;

            result.EmployeeVariances.Add(new EmployeeVariance
            {
                EmployeeId = ledger.EmployeeId,
                EmployeeName = ledger.EmployeeName,
                OriginalFnpf = origTotalFnpf,
                SimulatedFnpf = simTotalFnpf,
                OriginalPaye = ledger.PAYE.ToFijiRound(),
                SimulatedPaye = simPaye
            });
        }

        _logger.LogInformation(
            "Rule simulation complete. FNPF Variance: {FnpfVar:N2}, PAYE Variance: {PayeVar:N2}",
            result.FnpfVariance,
            result.PayeVariance);

        return result;
    }

    private async Task<decimal> ResolveRateAsync(
        string ruleCode,
        decimal defaultRate,
        Dictionary<string, string> overrides,
        DateTime date,
        CancellationToken cancellationToken)
    {
        if (overrides.TryGetValue(ruleCode, out string? valueStr) && decimal.TryParse(valueStr, out decimal ovDec))
        {
            return ovDec;
        }

        var rule = await _unitOfWork.Compliance.GetStatutoryRuleAsync("FNPF", ruleCode, date, cancellationToken)
            ?? await _unitOfWork.Compliance.GetStatutoryRuleAsync("FRCS", ruleCode, date, cancellationToken);

        if (rule != null && decimal.TryParse(rule.RuleValue, out decimal ruleDec))
        {
            return ruleDec;
        }

        return defaultRate;
    }
}
