using FijiPayroll.Domain.Entities.Payroll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Detail representing difference in a specific monetary component.
/// </summary>
public sealed record ComponentDifference(
    string ComponentCode,
    string ComponentName,
    string ComponentType,
    decimal AmountDiff
);

/// <summary>
/// Detailed differences for a single employee.
/// </summary>
public sealed record EmployeeDifference(
    int EmployeeId,
    string EmployeeName,
    decimal BaseSalaryDiff,
    decimal GrossPayDiff,
    decimal NetPayDiff,
    decimal TaxDiff,
    decimal FnpfDiff,
    IReadOnlyList<ComponentDifference> ComponentDifferences
);

/// <summary>
/// Metadata or rule/formula changes between runs.
/// </summary>
public sealed record MetadataDifference(
    string Key,
    string ValueA,
    string ValueB
);

/// <summary>
/// Report detailing differences between two payroll runs or snapshots.
/// </summary>
public sealed class PayrollDifferenceReport
{
    /// <summary>Total difference in Gross Pay.</summary>
    public decimal TotalGrossDifference { get; set; }

    /// <summary>Total difference in Net Pay.</summary>
    public decimal TotalNetDifference { get; set; }

    /// <summary>Total difference in PAYE Tax.</summary>
    public decimal TotalTaxDifference { get; set; }

    /// <summary>Total difference in FNPF contributions.</summary>
    public decimal TotalFnpfDifference { get; set; }

    /// <summary>Employee specific differences.</summary>
    public List<EmployeeDifference> EmployeeDifferences { get; } = new();

    /// <summary>Rule, formula or tax table differences.</summary>
    public List<MetadataDifference> MetadataDifferences { get; } = new();
}

/// <summary>
/// Difference analyzer service to compare runs/snapshots and report monetary/rule deltas.
/// </summary>
public sealed class PayrollDifferenceAnalyzer
{
    /// <summary>
    /// Compares two payroll runs side-by-side.
    /// </summary>
    public PayrollDifferenceReport CompareRuns(PayrollRun runA, PayrollRun runB)
    {
        if (runA == null) throw new ArgumentNullException(nameof(runA));
        if (runB == null) throw new ArgumentNullException(nameof(runB));

        var report = new PayrollDifferenceReport();

        // 1. Compare run level metadata
        var taxVersionA = runA.Employees.FirstOrDefault(e => !e.IsSuperseded)?.TaxVersionUsed ?? string.Empty;
        var taxVersionB = runB.Employees.FirstOrDefault(e => !e.IsSuperseded)?.TaxVersionUsed ?? string.Empty;
        if (taxVersionA != taxVersionB)
        {
            report.MetadataDifferences.Add(new MetadataDifference("TaxVersionUsed", taxVersionA, taxVersionB));
        }

        // Filter active employees (excluding superseded ones)
        var employeesA = runA.Employees.Where(e => !e.IsSuperseded).ToDictionary(e => e.EmployeeId);
        var employeesB = runB.Employees.Where(e => !e.IsSuperseded).ToDictionary(e => e.EmployeeId);

        var allEmployeeIds = employeesA.Keys.Union(employeesB.Keys).ToList();

        foreach (var empId in allEmployeeIds)
        {
            employeesA.TryGetValue(empId, out var empA);
            employeesB.TryGetValue(empId, out var empB);

            if (empA != null && empB != null)
            {
                // Employee is in both - compute differences
                decimal baseSalaryDiff = Math.Round(empB.BaseSalary - empA.BaseSalary, 2, MidpointRounding.AwayFromZero);
                decimal grossPayDiff = Math.Round(empB.GrossPay - empA.GrossPay, 2, MidpointRounding.AwayFromZero);
                decimal netPayDiff = Math.Round(empB.NetPay - empA.NetPay, 2, MidpointRounding.AwayFromZero);
                decimal taxDiff = Math.Round(empB.PayeTax - empA.PayeTax, 2, MidpointRounding.AwayFromZero);
                decimal fnpfDiff = Math.Round((empB.FnpfEmployeeContribution + empB.FnpfEmployerContribution) - 
                                              (empA.FnpfEmployeeContribution + empA.FnpfEmployerContribution), 2, MidpointRounding.AwayFromZero);

                var compDiffs = new List<ComponentDifference>();

                // Compare line items by component code
                var linesA = empA.LineItems.ToDictionary(l => l.ComponentCode, StringComparer.OrdinalIgnoreCase);
                var linesB = empB.LineItems.ToDictionary(l => l.ComponentCode, StringComparer.OrdinalIgnoreCase);

                var allCompCodes = linesA.Keys.Union(linesB.Keys, StringComparer.OrdinalIgnoreCase).ToList();

                foreach (var code in allCompCodes)
                {
                    linesA.TryGetValue(code, out var lineA);
                    linesB.TryGetValue(code, out var lineB);

                    decimal amtA = lineA?.Amount ?? 0m;
                    decimal amtB = lineB?.Amount ?? 0m;
                    decimal diff = Math.Round(amtB - amtA, 2, MidpointRounding.AwayFromZero);

                    if (diff != 0)
                    {
                        var name = lineB?.ComponentName ?? lineA?.ComponentName ?? code;
                        var type = (lineB?.ComponentType ?? lineA?.ComponentType ?? FijiPayroll.Domain.Enumerations.ComponentType.Allowance).ToString();
                        compDiffs.Add(new ComponentDifference(code, name, type, diff));
                    }
                }

                if (baseSalaryDiff != 0 || grossPayDiff != 0 || netPayDiff != 0 || taxDiff != 0 || fnpfDiff != 0 || compDiffs.Count > 0)
                {
                    report.EmployeeDifferences.Add(new EmployeeDifference(
                        empId,
                        empB.EmployeeName,
                        baseSalaryDiff,
                        grossPayDiff,
                        netPayDiff,
                        taxDiff,
                        fnpfDiff,
                        compDiffs.AsReadOnly()
                    ));
                }
            }
            else if (empA != null)
            {
                // Removed in B
                decimal baseSalaryDiff = Math.Round(-empA.BaseSalary, 2, MidpointRounding.AwayFromZero);
                decimal grossPayDiff = Math.Round(-empA.GrossPay, 2, MidpointRounding.AwayFromZero);
                decimal netPayDiff = Math.Round(-empA.NetPay, 2, MidpointRounding.AwayFromZero);
                decimal taxDiff = Math.Round(-empA.PayeTax, 2, MidpointRounding.AwayFromZero);
                decimal fnpfDiff = Math.Round(-(empA.FnpfEmployeeContribution + empA.FnpfEmployerContribution), 2, MidpointRounding.AwayFromZero);

                var compDiffs = empA.LineItems.Select(l => new ComponentDifference(
                    l.ComponentCode,
                    l.ComponentName,
                    l.ComponentType.ToString(),
                    Math.Round(-l.Amount, 2, MidpointRounding.AwayFromZero)
                )).ToList();

                report.EmployeeDifferences.Add(new EmployeeDifference(
                    empId,
                    empA.EmployeeName,
                    baseSalaryDiff,
                    grossPayDiff,
                    netPayDiff,
                    taxDiff,
                    fnpfDiff,
                    compDiffs.AsReadOnly()
                ));
            }
            else if (empB != null)
            {
                // Added in B
                decimal baseSalaryDiff = Math.Round(empB.BaseSalary, 2, MidpointRounding.AwayFromZero);
                decimal grossPayDiff = Math.Round(empB.GrossPay, 2, MidpointRounding.AwayFromZero);
                decimal netPayDiff = Math.Round(empB.NetPay, 2, MidpointRounding.AwayFromZero);
                decimal taxDiff = Math.Round(empB.PayeTax, 2, MidpointRounding.AwayFromZero);
                decimal fnpfDiff = Math.Round(empB.FnpfEmployeeContribution + empB.FnpfEmployerContribution, 2, MidpointRounding.AwayFromZero);

                var compDiffs = empB.LineItems.Select(l => new ComponentDifference(
                    l.ComponentCode,
                    l.ComponentName,
                    l.ComponentType.ToString(),
                    Math.Round(l.Amount, 2, MidpointRounding.AwayFromZero)
                )).ToList();

                report.EmployeeDifferences.Add(new EmployeeDifference(
                    empId,
                    empB.EmployeeName,
                    baseSalaryDiff,
                    grossPayDiff,
                    netPayDiff,
                    taxDiff,
                    fnpfDiff,
                    compDiffs.AsReadOnly()
                ));
            }
        }

        // Summarize
        report.TotalGrossDifference = Math.Round(report.EmployeeDifferences.Sum(d => d.GrossPayDiff), 2, MidpointRounding.AwayFromZero);
        report.TotalNetDifference = Math.Round(report.EmployeeDifferences.Sum(d => d.NetPayDiff), 2, MidpointRounding.AwayFromZero);
        report.TotalTaxDifference = Math.Round(report.EmployeeDifferences.Sum(d => d.TaxDiff), 2, MidpointRounding.AwayFromZero);
        report.TotalFnpfDifference = Math.Round(report.EmployeeDifferences.Sum(d => d.FnpfDiff), 2, MidpointRounding.AwayFromZero);

        return report;
    }

    /// <summary>
    /// Compares two snapshots side-by-side by parsing their context payloads.
    /// </summary>
    public PayrollDifferenceReport CompareSnapshots(PayrollSnapshot snapA, PayrollSnapshot snapB)
    {
        if (snapA == null) throw new ArgumentNullException(nameof(snapA));
        if (snapB == null) throw new ArgumentNullException(nameof(snapB));

        var contextA = JsonSerializer.Deserialize<PayrollExecutionContext>(snapA.JsonPayload);
        var contextB = JsonSerializer.Deserialize<PayrollExecutionContext>(snapB.JsonPayload);

        if (contextA == null || contextB == null)
        {
            throw new InvalidOperationException("Failed to deserialize one or both snapshots.");
        }

        var report = new PayrollDifferenceReport();

        // 1. Compare global metadata & versions
        if (contextA.TaxVersion != contextB.TaxVersion)
        {
            report.MetadataDifferences.Add(new MetadataDifference("TaxVersion", contextA.TaxVersion, contextB.TaxVersion));
        }

        // 2. Compare rule/formula versions and components
        var compsA = contextA.Components.ToDictionary(c => c.ComponentCode, StringComparer.OrdinalIgnoreCase);
        var compsB = contextB.Components.ToDictionary(c => c.ComponentCode, StringComparer.OrdinalIgnoreCase);

        var allCompCodes = compsA.Keys.Union(compsB.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var code in allCompCodes)
        {
            compsA.TryGetValue(code, out var cA);
            compsB.TryGetValue(code, out var cB);

            if (cA != null && cB != null)
            {
                if (cA.CalculationMethod != cB.CalculationMethod)
                {
                    report.MetadataDifferences.Add(new MetadataDifference($"Component_{code}_Method", cA.CalculationMethod.ToString(), cB.CalculationMethod.ToString()));
                }
                if (cA.CalculationValue != cB.CalculationValue)
                {
                    report.MetadataDifferences.Add(new MetadataDifference($"Component_{code}_Value", cA.CalculationValue?.ToString() ?? "null", cB.CalculationValue?.ToString() ?? "null"));
                }
                if (cA.Formula != cB.Formula)
                {
                    report.MetadataDifferences.Add(new MetadataDifference($"Component_{code}_Formula", cA.Formula ?? "null", cB.Formula ?? "null"));
                }
            }
            else if (cA != null)
            {
                report.MetadataDifferences.Add(new MetadataDifference($"Component_{code}_Status", "Present", "Removed"));
            }
            else if (cB != null)
            {
                report.MetadataDifferences.Add(new MetadataDifference($"Component_{code}_Status", "NotPresent", "Added"));
            }
        }

        // 3. Compare employees
        var empsA = contextA.Employees.ToDictionary(e => e.EmployeeId);
        var empsB = contextB.Employees.ToDictionary(e => e.EmployeeId);

        var allEmpIds = empsA.Keys.Union(empsB.Keys).ToList();

        foreach (var empId in allEmpIds)
        {
            empsA.TryGetValue(empId, out var empA);
            empsB.TryGetValue(empId, out var empB);

            if (empA != null && empB != null)
            {
                decimal baseSalaryDiff = Math.Round(empB.BaseSalary - empA.BaseSalary, 2, MidpointRounding.AwayFromZero);
                var overridesDiff = new List<ComponentDifference>();

                // Compare overrides
                var ovsA = empA.ComponentOverrides.ToDictionary(o => o.ComponentCode, StringComparer.OrdinalIgnoreCase);
                var ovsB = empB.ComponentOverrides.ToDictionary(o => o.ComponentCode, StringComparer.OrdinalIgnoreCase);

                var ovCodes = ovsA.Keys.Union(ovsB.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var ovCode in ovCodes)
                {
                    ovsA.TryGetValue(ovCode, out var ovA);
                    ovsB.TryGetValue(ovCode, out var ovB);

                    decimal valA = ovA?.Value ?? 0m;
                    decimal valB = ovB?.Value ?? 0m;
                    decimal diff = Math.Round(valB - valA, 2, MidpointRounding.AwayFromZero);

                    if (diff != 0)
                    {
                        overridesDiff.Add(new ComponentDifference(ovCode, ovCode, "Override", diff));
                    }
                }

                if (baseSalaryDiff != 0 || overridesDiff.Count > 0)
                {
                    report.EmployeeDifferences.Add(new EmployeeDifference(
                        empId,
                        empB.FullName,
                        baseSalaryDiff,
                        0m, // Snapshot input does not contain final calculated gross/net directly unless calculated
                        0m,
                        0m,
                        0m,
                        overridesDiff.AsReadOnly()
                    ));
                }
            }
            else if (empA != null)
            {
                report.EmployeeDifferences.Add(new EmployeeDifference(
                    empId,
                    empA.FullName,
                    Math.Round(-empA.BaseSalary, 2, MidpointRounding.AwayFromZero),
                    0m, 0m, 0m, 0m,
                    Array.Empty<ComponentDifference>()
                ));
            }
            else if (empB != null)
            {
                report.EmployeeDifferences.Add(new EmployeeDifference(
                    empId,
                    empB.FullName,
                    Math.Round(empB.BaseSalary, 2, MidpointRounding.AwayFromZero),
                    0m, 0m, 0m, 0m,
                    Array.Empty<ComponentDifference>()
                ));
            }
        }

        return report;
    }
}
