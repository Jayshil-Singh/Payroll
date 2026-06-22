using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Result detail of a payroll validation check.
/// </summary>
public sealed record PayrollValidationIssue(
    int? EmployeeId,
    string EmployeeName,
    string RuleCode,
    string Message,
    PayrollValidationSeverity Severity,
    string Recommendation
);

/// <summary>
/// Pipeline performing statutory completeness, duplicate identification, and banking checks.
/// </summary>
public sealed class PayrollValidationPipeline
{
    /// <summary>
    /// Runs validation checks over the list of employees and configurations.
    /// </summary>
    public IReadOnlyList<PayrollValidationIssue> Validate(
        int companyId,
        IEnumerable<Employee> employees,
        string ruleVersion,
        string formulaVersion)
    {
        var issues = new List<PayrollValidationIssue>();
        var employeeList = employees.ToList();

        if (string.IsNullOrWhiteSpace(ruleVersion))
        {
            issues.Add(new PayrollValidationIssue(
                null,
                "System",
                "RULE_VERSION_MISSING",
                "Statutory rules version is not specified.",
                PayrollValidationSeverity.Critical,
                "Configure a valid statutory rule package version before calculations."));
        }

        if (string.IsNullOrWhiteSpace(formulaVersion))
        {
            issues.Add(new PayrollValidationIssue(
                null,
                "System",
                "FORMULA_VERSION_MISSING",
                "Statutory formula version is not specified.",
                PayrollValidationSeverity.Critical,
                "Configure a valid formula engine version."));
        }

        // Keep track for duplicates
        var tinsSeen = new Dictionary<string, List<Employee>>(StringComparer.OrdinalIgnoreCase);
        var fnpfsSeen = new Dictionary<string, List<Employee>>(StringComparer.OrdinalIgnoreCase);
        var idsSeen = new HashSet<int>();

        foreach (var emp in employeeList)
        {
            // ID Check
            if (idsSeen.Contains(emp.Id))
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "DUPLICATE_EMPLOYEE_ID",
                    $"Employee ID {emp.Id} is duplicate in the processing queue.",
                    PayrollValidationSeverity.Critical,
                    "Remove duplicate entry from the run list."));
            }
            else
            {
                idsSeen.Add(emp.Id);
            }

            // Salary Check
            if (emp.BaseSalary < 0)
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "NEGATIVE_SALARY",
                    $"Employee {emp.FullName} has a negative base salary: {emp.BaseSalary}.",
                    PayrollValidationSeverity.Critical,
                    "Correct the salary amount to be greater than or equal to zero."));
            }

            // TIN Check
            if (string.IsNullOrWhiteSpace(emp.Tin))
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "TIN_MISSING",
                    $"Employee {emp.FullName} is missing a Tax Identification Number (TIN).",
                    PayrollValidationSeverity.Error,
                    "Provide a valid 9-digit TIN for FRCS reporting."));
            }
            else
            {
                string cleanTin = emp.Tin.Trim();
                if (cleanTin.Length != 9 || !cleanTin.All(char.IsDigit))
                {
                    issues.Add(new PayrollValidationIssue(
                        emp.Id,
                        emp.FullName,
                        "TIN_INVALID",
                        $"Employee {emp.FullName} has an invalid TIN: '{emp.Tin}'. Must be exactly 9 digits.",
                        PayrollValidationSeverity.Error,
                        "Correct the TIN to exactly 9 digits."));
                }
                else
                {
                    if (!tinsSeen.ContainsKey(cleanTin))
                    {
                        tinsSeen[cleanTin] = new List<Employee>();
                    }
                    tinsSeen[cleanTin].Add(emp);
                }
            }

            // FNPF Check
            if (string.IsNullOrWhiteSpace(emp.FnpfNumber))
            {
                if (!emp.IsFnpfExempt)
                {
                    issues.Add(new PayrollValidationIssue(
                        emp.Id,
                        emp.FullName,
                        "FNPF_MISSING",
                        $"Employee {emp.FullName} is not FNPF-exempt but lacks an FNPF registration number.",
                        PayrollValidationSeverity.Error,
                        "Provide a valid FNPF registration number."));
                }
            }
            else
            {
                string cleanFnpf = emp.FnpfNumber.Trim();
                if (!tinsSeen.ContainsKey(cleanFnpf)) // Duplicate check map
                {
                    if (!fnpfsSeen.ContainsKey(cleanFnpf))
                    {
                        fnpfsSeen[cleanFnpf] = new List<Employee>();
                    }
                    fnpfsSeen[cleanFnpf].Add(emp);
                }
            }

            // Bank routing details
            var primaryBank = emp.PaymentMethods.FirstOrDefault(pm => pm.IsPrimary);
            if (primaryBank == null || string.IsNullOrWhiteSpace(primaryBank.BankAccountNumber))
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "BANK_ACCOUNT_MISSING",
                    $"Employee {emp.FullName} is missing a bank account number.",
                    PayrollValidationSeverity.Warning,
                    "Update banking details if disbursement is executed electronically."));
            }

            // Cost Centre details
            if (string.IsNullOrWhiteSpace(emp.CostCentre))
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "COST_CENTRE_MISSING",
                    $"Employee {emp.FullName} is missing a Cost Centre setting.",
                    PayrollValidationSeverity.Warning,
                    "Configure Cost Centre for financial costing reports."));
            }

            // Department details
            if (string.IsNullOrWhiteSpace(emp.Department))
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "DEPARTMENT_MISSING",
                    $"Employee {emp.FullName} has no department configured.",
                    PayrollValidationSeverity.Info,
                    "Assign a department to organize employee hierarchy."));
            }
        }

        // Duplicate TIN checks
        foreach (var pair in tinsSeen.Where(p => p.Value.Count > 1))
        {
            foreach (var emp in pair.Value)
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "DUPLICATE_TIN",
                    $"Duplicate TIN '{pair.Key}' is shared with other employees.",
                    PayrollValidationSeverity.Critical,
                    "Ensure every employee has a unique TIN."));
            }
        }

        // Duplicate FNPF checks
        foreach (var pair in fnpfsSeen.Where(p => p.Value.Count > 1))
        {
            foreach (var emp in pair.Value)
            {
                issues.Add(new PayrollValidationIssue(
                    emp.Id,
                    emp.FullName,
                    "DUPLICATE_FNPF",
                    $"Duplicate FNPF number '{pair.Key}' is shared with other employees.",
                    PayrollValidationSeverity.Critical,
                    "Ensure every employee has a unique FNPF registration."));
            }
        }

        return issues.AsReadOnly();
    }
}
