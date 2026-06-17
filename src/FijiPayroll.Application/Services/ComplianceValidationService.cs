using System;
using System.Collections.Generic;
using System.Linq;
using FijiPayroll.SDK.Contracts;
using FijiPayroll.SDK.Interfaces;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Composite validation engine implementing compliance validation pipelines over payroll runs, returns, and bank files.
/// </summary>
public sealed class ComplianceValidationService
{
    private readonly IEnumerable<IComplianceValidator> _validators;
    private readonly ILogger<ComplianceValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceValidationService"/> class.
    /// </summary>
    public ComplianceValidationService(
        IEnumerable<IComplianceValidator> validators,
        ILogger<ComplianceValidationService> logger)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Evaluates a target compliance dataset against all registered validation rule checks.
    /// </summary>
    /// <param name="companyId">The company tenant context identifier.</param>
    /// <param name="payload">The dataset to validate (e.g. enumerable of PaymentDetail or a payroll run).</param>
    /// <returns>A sequence of validation warning and error issues.</returns>
    public IEnumerable<ValidationIssue> ValidateDataset(int companyId, object payload)
    {
        _logger.LogInformation("Running composite compliance validation pipeline for Company {CompanyId}", companyId);
        
        var issues = new List<ValidationIssue>();

        // 1. Run custom registered validators from plugins or system boundaries
        foreach (var validator in _validators)
        {
            try
            {
                var valIssues = validator.Validate(companyId, payload);
                if (valIssues != null)
                {
                    issues.AddRange(valIssues);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing validator '{RuleCode}'", validator.RuleCode);
            }
        }

        // 2. Fallback/Built-in baseline compliance rules if checking PaymentDetail records
        if (payload is IEnumerable<PaymentDetail> payments)
        {
            foreach (var payment in payments)
            {
                // TIN Checks
                if (string.IsNullOrWhiteSpace(payment.Tin))
                {
                    issues.Add(new ValidationIssue(
                        Severity: "Error",
                        Message: "Employee tax identification number (TIN) is missing.",
                        AffectedEmployee: payment.EmployeeName,
                        RuleCode: "FRCS_TIN_MISSING",
                        RecommendedAction: "Provide a valid 9-digit TIN in employee settings."
                    ));
                }
                else if (payment.Tin.Length != 9 || !payment.Tin.All(char.IsDigit))
                {
                    issues.Add(new ValidationIssue(
                        Severity: "Error",
                        Message: $"Employee TIN '{payment.Tin}' is invalid. TIN must be exactly 9 digits.",
                        AffectedEmployee: payment.EmployeeName,
                        RuleCode: "FRCS_TIN_INVALID",
                        RecommendedAction: "Update employee record with a valid 9-digit TIN."
                    ));
                }

                // FNPF Number Checks
                if (payment.FnpfEmployee > 0 && string.IsNullOrWhiteSpace(payment.FnpfNumber))
                {
                    issues.Add(new ValidationIssue(
                        Severity: "Error",
                        Message: "Employee FNPF registration number is missing for contribution deduction.",
                        AffectedEmployee: payment.EmployeeName,
                        RuleCode: "FNPF_NUM_MISSING",
                        RecommendedAction: "Verify employee FNPF number in onboarding details."
                    ));
                }

                // Bank Account/BSB Checks
                if (string.IsNullOrWhiteSpace(payment.BankAccountNumber))
                {
                    issues.Add(new ValidationIssue(
                        Severity: "Warning",
                        Message: "Bank account number is empty; employee payout will be skipped in bank clearing file.",
                        AffectedEmployee: payment.EmployeeName,
                        RuleCode: "BANK_ACCT_MISSING",
                        RecommendedAction: "Enter bank account details if employee is paid via direct credit."
                    ));
                }
            }
        }

        _logger.LogInformation("Compliance validation pipeline complete. Found {IssueCount} issues.", issues.Count);
        return issues;
    }
}
