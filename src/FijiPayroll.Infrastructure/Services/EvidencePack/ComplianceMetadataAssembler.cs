using System;
using System.Collections.Generic;
using System.Linq;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.SDK.Contracts;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Infrastructure assembler responsible for gathering compliance validations and summarizing step-level employee traces.
/// </summary>
public sealed class ComplianceMetadataAssembler
{
    private readonly ComplianceValidationService _validationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceMetadataAssembler"/> class.
    /// </summary>
    public ComplianceMetadataAssembler(ComplianceValidationService validationService)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    /// <summary>
    /// Evaluates the ledger records against statutory FRCS/FNPF compliance rules.
    /// </summary>
    public ComplianceValidationOutput AssembleValidationResults(int companyId, IEnumerable<PayrollLedger> ledgers)
    {
        if (ledgers == null) throw new ArgumentNullException(nameof(ledgers));

        // Map PayrollLedgers to SDK PaymentDetail objects
        var paymentDetails = ledgers.Select(l => new PaymentDetail(
            EmployeeId: l.EmployeeId,
            EmployeeName: l.EmployeeName,
            Bsb: string.Empty,
            AccountNumber: string.Empty,
            Amount: l.NetPay,
            Reference: $"Run-{l.PayrollRunId}",
            Tin: l.EmployeeTin,
            Gross: l.Gross,
            Paye: l.PAYE,
            FnpfNumber: l.EmployeeFnpfNumber,
            FnpfEmployee: l.FNPFEmployee,
            FnpfEmployer: l.FNPFEmployer,
            BankAccountNumber: string.Empty,
            EmployeeCode: l.EmployeeId.ToString()
        )).ToList();

        // Run the system's compliance validator composite pipeline
        var rawIssues = _validationService.ValidateDataset(companyId, paymentDetails) ?? Enumerable.Empty<ValidationIssue>();

        var frcsIssues = new List<EvidenceValidationIssue>();
        var fnpfIssues = new List<EvidenceValidationIssue>();

        foreach (var issue in rawIssues)
        {
            var evidenceIssue = new EvidenceValidationIssue(
                Severity: issue.Severity,
                Message: issue.Message,
                AffectedEmployee: issue.AffectedEmployee,
                RuleCode: issue.RuleCode,
                RecommendedAction: issue.RecommendedAction
            );

            if (issue.RuleCode.StartsWith("FNPF_", StringComparison.OrdinalIgnoreCase))
            {
                fnpfIssues.Add(evidenceIssue);
            }
            else
            {
                frcsIssues.Add(evidenceIssue);
            }
        }

        return new ComplianceValidationOutput(
            FrcsValidationResults: frcsIssues.AsReadOnly(),
            FnpfValidationResults: fnpfIssues.AsReadOnly()
        );
    }

    /// <summary>
    /// Summarizes the execution traces from PayrollRunEmployee records.
    /// </summary>
    public TraceabilityEvidence AssembleTraceability(IEnumerable<PayrollRunEmployee> runEmployees)
    {
        if (runEmployees == null) throw new ArgumentNullException(nameof(runEmployees));

        var tracesList = new List<EmployeeTraceEvidence>();

        // Sort by EmployeeId for determinism
        var sortedRunEmps = runEmployees.OrderBy(x => x.EmployeeId).ToList();

        foreach (var emp in sortedRunEmps)
        {
            string rawTraceText = emp.Trace?.TraceText ?? string.Empty;

            var orderedSteps = new List<string>();
            var summaryLines = new List<string>();

            // Parse trace step milestones deterministically
            var lines = rawTraceText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string cleanLine = line.Trim();
                if (cleanLine.Contains("[Trace]", StringComparison.OrdinalIgnoreCase))
                {
                    // Clean up the "[Trace] " prefix
                    string text = cleanLine.Replace("[Trace]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    summaryLines.Add(text);

                    // Check if component code is inside parenthesis, e.g. "(BASIC)" or "(OT)"
                    int openIdx = text.LastIndexOf('(');
                    int closeIdx = text.LastIndexOf(')');
                    if (openIdx >= 0 && closeIdx > openIdx)
                    {
                        string possibleCode = text.Substring(openIdx + 1, closeIdx - openIdx - 1).Trim().ToUpperInvariant();
                        if (!string.IsNullOrWhiteSpace(possibleCode) && !orderedSteps.Contains(possibleCode))
                        {
                            orderedSteps.Add(possibleCode);
                        }
                    }
                }
            }

            // Fallback if no specific [Trace] lines were parsed
            if (summaryLines.Count == 0 && !string.IsNullOrWhiteSpace(rawTraceText))
            {
                summaryLines.Add("Raw execution trace recorded.");
            }

            // Assemble component values map from the actual line items
            var componentValues = emp.LineItems
                .Where(x => !string.IsNullOrWhiteSpace(x.ComponentCode))
                .GroupBy(x => x.ComponentCode)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Amount
                );

            var traceEvidence = new EmployeeTraceEvidence(
                EmployeeId: emp.EmployeeId,
                EmployeeName: emp.EmployeeName,
                TraceTextSummary: string.Join("\n", summaryLines),
                OrderedStepReferenceIds: orderedSteps.AsReadOnly(),
                ComponentValues: componentValues
            );

            tracesList.Add(traceEvidence);
        }

        return new TraceabilityEvidence(EmployeeTraces: tracesList.AsReadOnly());
    }
}
