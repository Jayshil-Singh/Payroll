using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Rules.PayrollRules;
using FijiPayroll.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Stateless pure calculation orchestrator. Contains NO database access or persistence triggers.
/// Executes calculations entirely in-memory using PayrollExecutionContext snapshots.
/// </summary>
public sealed class PayrollCalculationEngine
{
    /// <summary>
    /// Executes the payroll run calculations on the immutable context snapshot, returning the results snapshot.
    /// </summary>
    public CalculationResultSnapshot Calculate(PayrollExecutionContext context)
    {
        var employeeResults = new List<CalculatedEmployeeResult>();

        // ORDERING RULE: Calculations must use ordered list by EmployeeId.
        // We enforce sorting by EmployeeId here as well to guarantee determinism.
        var sortedEmployees = context.Employees.OrderBy(e => e.EmployeeId).ToList();

        foreach (var emp in sortedEmployees)
        {
            var trace = new StringBuilder();
            trace.AppendLine($"[Trace] Starting payroll calculation for Employee ID: {emp.EmployeeId}, Name: {emp.FullName}");
            trace.AppendLine($"[Trace] Base Salary: {emp.BaseSalary:C}");
            trace.AppendLine($"[Trace] Frequency: {context.Frequency}");
            trace.AppendLine($"[Trace] Residency: {emp.ResidencyStatus}");

            decimal basicSalary = Math.Round(emp.BaseSalary, 2, MidpointRounding.AwayFromZero);
            decimal totalEarnings = basicSalary;
            decimal totalAllowances = 0;
            decimal totalDeductions = 0;

            var calculatedLines = new List<CalculatedLineItemResult>();

            // Find basic salary component configuration
            var basicComp = context.Components.FirstOrDefault(c => 
                c.ComponentCode.Equals("BASIC", StringComparison.OrdinalIgnoreCase) || 
                c.ComponentCode.Equals(FijiTaxConstants.BasicSalaryComponentCode, StringComparison.OrdinalIgnoreCase));
            if (basicComp != null)
            {
                calculatedLines.Add(new CalculatedLineItemResult
                {
                    ComponentId = basicComp.Id,
                    ComponentCode = basicComp.ComponentCode,
                    ComponentName = basicComp.ComponentName,
                    ComponentType = basicComp.ComponentType,
                    Amount = basicSalary,
                    IsTaxable = basicComp.IsTaxable,
                    AffectsFnpf = basicComp.IsFnpfApplicable,
                    EmployerContributionFlag = false,
                    ReferenceComponentId = basicComp.Id
                });
                trace.AppendLine($"[Trace] Added basic salary component: {basicSalary:C}");
            }

            // Calculate active allowances / earnings components
            var activeAllowancesAndEarnings = context.Components
                .Where(c => c.ComponentType == ComponentType.Allowance 
                         || (c.ComponentType == ComponentType.Earning 
                             && !c.ComponentCode.Equals("BASIC", StringComparison.OrdinalIgnoreCase)
                             && !c.ComponentCode.Equals(FijiTaxConstants.BasicSalaryComponentCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var comp in activeAllowancesAndEarnings)
            {
                var manualOverride = emp.ComponentOverrides.FirstOrDefault(o => o.ComponentCode.Equals(comp.ComponentCode, StringComparison.OrdinalIgnoreCase));
                decimal? overrideVal = manualOverride?.Value;

                decimal amount = PayrollAllowanceEngine.CalculateAllowance(
                    comp.CalculationMethod,
                    comp.CalculationValue,
                    comp.Formula,
                    basicSalary,
                    emp.HoursWorked,
                    emp.OvertimeHours,
                    overrideVal
                );

                if (amount != 0)
                {
                    calculatedLines.Add(new CalculatedLineItemResult
                    {
                        ComponentId = comp.Id,
                        ComponentCode = comp.ComponentCode,
                        ComponentName = comp.ComponentName,
                        ComponentType = comp.ComponentType,
                        Amount = amount,
                        IsTaxable = comp.IsTaxable,
                        AffectsFnpf = comp.IsFnpfApplicable,
                        EmployerContributionFlag = false,
                        ReferenceComponentId = comp.Id
                    });

                    if (comp.ComponentType == ComponentType.Allowance)
                    {
                        totalAllowances += amount;
                        trace.AppendLine($"[Trace] Calculated Allowance '{comp.ComponentName}' ({comp.ComponentCode}): {amount:C}");
                    }
                    else
                    {
                        totalEarnings += amount;
                        trace.AppendLine($"[Trace] Calculated Earning '{comp.ComponentName}' ({comp.ComponentCode}): {amount:C}");
                    }
                }
            }

            // Calculate FNPF-applicable gross
            decimal fnpfApplicableGross = basicSalary;
            foreach (var line in calculatedLines.Where(l => l.AffectsFnpf))
            {
                if (!line.ComponentCode.Equals("BASIC", StringComparison.OrdinalIgnoreCase)
                 && !line.ComponentCode.Equals(FijiTaxConstants.BasicSalaryComponentCode, StringComparison.OrdinalIgnoreCase))
                {
                    fnpfApplicableGross += line.Amount;
                }
            }
            trace.AppendLine($"[Trace] FNPF Applicable Gross: {fnpfApplicableGross:C}");

            // Calculate FNPF employee & employer contributions
            decimal fnpfEmployee = PayrollDeductionEngine.CalculateEmployeeFnpf(fnpfApplicableGross, emp.IsFnpfExempt);
            decimal fnpfEmployer = PayrollDeductionEngine.CalculateEmployerFnpf(fnpfApplicableGross, emp.IsFnpfExempt);

            var fnpfEmpComp = context.Components.FirstOrDefault(c => 
                c.ComponentCode.Equals("FNPF_EE", StringComparison.OrdinalIgnoreCase) || 
                c.ComponentCode.Equals("FNPF-EMP", StringComparison.OrdinalIgnoreCase) || 
                c.ComponentCode.Equals(FijiTaxConstants.FnpfEmployeeComponentCode, StringComparison.OrdinalIgnoreCase));
            if (fnpfEmpComp != null && fnpfEmployee > 0)
            {
                calculatedLines.Add(new CalculatedLineItemResult
                {
                    ComponentId = fnpfEmpComp.Id,
                    ComponentCode = fnpfEmpComp.ComponentCode,
                    ComponentName = fnpfEmpComp.ComponentName,
                    ComponentType = fnpfEmpComp.ComponentType,
                    Amount = -fnpfEmployee,
                    IsTaxable = fnpfEmpComp.IsTaxable,
                    AffectsFnpf = fnpfEmpComp.IsFnpfApplicable,
                    EmployerContributionFlag = false,
                    ReferenceComponentId = fnpfEmpComp.Id
                });
                totalDeductions += fnpfEmployee;
                trace.AppendLine($"[Trace] FNPF Employee contribution (8%): {fnpfEmployee:C}");
            }

            var fnpfEmplrComp = context.Components.FirstOrDefault(c => 
                c.ComponentCode.Equals("FNPF_ER", StringComparison.OrdinalIgnoreCase) || 
                c.ComponentCode.Equals("FNPF-EMPLR", StringComparison.OrdinalIgnoreCase) || 
                c.ComponentCode.Equals(FijiTaxConstants.FnpfEmployerComponentCode, StringComparison.OrdinalIgnoreCase));
            if (fnpfEmplrComp != null && fnpfEmployer > 0)
            {
                calculatedLines.Add(new CalculatedLineItemResult
                {
                    ComponentId = fnpfEmplrComp.Id,
                    ComponentCode = fnpfEmplrComp.ComponentCode,
                    ComponentName = fnpfEmplrComp.ComponentName,
                    ComponentType = fnpfEmplrComp.ComponentType,
                    Amount = fnpfEmployer,
                    IsTaxable = fnpfEmplrComp.IsTaxable,
                    AffectsFnpf = fnpfEmplrComp.IsFnpfApplicable,
                    EmployerContributionFlag = true,
                    ReferenceComponentId = fnpfEmplrComp.Id
                });
                trace.AppendLine($"[Trace] FNPF Employer contribution (10%): {fnpfEmployer:C}");
            }

            // Calculate Taxable Income for PAYE
            decimal periodTaxableGross = basicSalary;
            foreach (var line in calculatedLines.Where(l => l.IsTaxable && !l.EmployerContributionFlag))
            {
                if (!line.ComponentCode.Equals("BASIC", StringComparison.OrdinalIgnoreCase)
                 && !line.ComponentCode.Equals(FijiTaxConstants.BasicSalaryComponentCode, StringComparison.OrdinalIgnoreCase))
                {
                    periodTaxableGross += line.Amount;
                }
            }
            trace.AppendLine($"[Trace] Period Taxable Gross: {periodTaxableGross:C}");

            // Calculate PAYE Tax
            decimal payeTax = 0;
            if (!emp.IsTaxExempt)
            {
                var mappedBrackets = context.TaxRules.ToList();

                payeTax = PayrollTaxEngine.CalculatePaye(
                    periodTaxableGross,
                    fnpfEmployee,
                    context.Frequency,
                    emp.ResidencyStatus,
                    context.TaxVersion,
                    mappedBrackets
                );
            }
            else
            {
                trace.AppendLine("[Trace] Employee is tax-exempt.");
            }

            var payeComp = context.Components.FirstOrDefault(c => 
                c.ComponentCode.Equals("PAYE", StringComparison.OrdinalIgnoreCase) || 
                c.ComponentCode.Equals(FijiTaxConstants.PayeComponentCode, StringComparison.OrdinalIgnoreCase));
            if (payeComp != null && payeTax > 0)
            {
                calculatedLines.Add(new CalculatedLineItemResult
                {
                    ComponentId = payeComp.Id,
                    ComponentCode = payeComp.ComponentCode,
                    ComponentName = payeComp.ComponentName,
                    ComponentType = payeComp.ComponentType,
                    Amount = -payeTax,
                    IsTaxable = payeComp.IsTaxable,
                    AffectsFnpf = payeComp.IsFnpfApplicable,
                    EmployerContributionFlag = false,
                    ReferenceComponentId = payeComp.Id
                });
                totalDeductions += payeTax;
                trace.AppendLine($"[Trace] PAYE tax calculated: {payeTax:C}");
            }

            // Process non-statutory/voluntary deductions
            var deductionComponents = context.Components
                .Where(c => c.ComponentType == ComponentType.Deduction 
                         && !c.ComponentCode.Equals("FNPF_EE", StringComparison.OrdinalIgnoreCase)
                         && !c.ComponentCode.Equals("FNPF-EMP", StringComparison.OrdinalIgnoreCase)
                         && !c.ComponentCode.Equals(FijiTaxConstants.FnpfEmployeeComponentCode, StringComparison.OrdinalIgnoreCase)
                         && !c.ComponentCode.Equals("PAYE", StringComparison.OrdinalIgnoreCase)
                         && !c.ComponentCode.Equals(FijiTaxConstants.PayeComponentCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var comp in deductionComponents)
            {
                var manualOverride = emp.ComponentOverrides.FirstOrDefault(o => o.ComponentCode.Equals(comp.ComponentCode, StringComparison.OrdinalIgnoreCase));
                decimal? overrideVal = manualOverride?.Value;

                decimal amount = PayrollAllowanceEngine.CalculateAllowance(
                    comp.CalculationMethod,
                    comp.CalculationValue,
                    comp.Formula,
                    basicSalary,
                    emp.HoursWorked,
                    emp.OvertimeHours,
                    overrideVal
                );

                if (amount != 0)
                {
                    calculatedLines.Add(new CalculatedLineItemResult
                    {
                        ComponentId = comp.Id,
                        ComponentCode = comp.ComponentCode,
                        ComponentName = comp.ComponentName,
                        ComponentType = comp.ComponentType,
                        Amount = -amount,
                        IsTaxable = comp.IsTaxable,
                        AffectsFnpf = comp.IsFnpfApplicable,
                        EmployerContributionFlag = false,
                        ReferenceComponentId = comp.Id
                    });
                    totalDeductions += amount;
                    trace.AppendLine($"[Trace] Calculated Deduction '{comp.ComponentName}' ({comp.ComponentCode}): {amount:C}");
                }
            }

            // Compute gross pay
            decimal grossPay = Math.Round(totalEarnings, 2, MidpointRounding.AwayFromZero);

            // Compute final net pay
            decimal netPay = Math.Round(grossPay + totalAllowances - totalDeductions, 2, MidpointRounding.AwayFromZero);
            trace.AppendLine($"[Trace] Calculated Net Pay raw: {netPay:C}");

            // Apply net pay floor rule: net pay must never be negative
            if (netPay < 0)
            {
                decimal shortfall = -netPay;
                // Find all voluntary (non-statutory) deduction line items
                var voluntaryDeductionLines = calculatedLines
                    .Where(l => l.ComponentType == ComponentType.Deduction
                             && !l.ComponentCode.Equals("FNPF_EE", StringComparison.OrdinalIgnoreCase)
                             && !l.ComponentCode.Equals("FNPF-EMP", StringComparison.OrdinalIgnoreCase)
                             && !l.ComponentCode.Equals(FijiTaxConstants.FnpfEmployeeComponentCode, StringComparison.OrdinalIgnoreCase)
                             && !l.ComponentCode.Equals("PAYE", StringComparison.OrdinalIgnoreCase)
                             && !l.ComponentCode.Equals(FijiTaxConstants.PayeComponentCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var line in voluntaryDeductionLines)
                {
                    if (shortfall <= 0) break;

                    decimal originalDeductionVal = -line.Amount;
                    if (originalDeductionVal <= 0) continue;

                    decimal reduction = Math.Round(Math.Min(originalDeductionVal, shortfall), 2, MidpointRounding.AwayFromZero);
                    line.Amount += reduction; // makes it closer to 0 (less negative)
                    totalDeductions = Math.Round(totalDeductions - reduction, 2, MidpointRounding.AwayFromZero);
                    shortfall = Math.Round(shortfall - reduction, 2, MidpointRounding.AwayFromZero);

                    trace.AppendLine($"[Warning] Net pay floor rule: Reduced voluntary deduction '{line.ComponentName}' ({line.ComponentCode}) by {reduction:C} (New: {-line.Amount:C}) to avoid negative net pay.");
                }

                // Recalculate net pay after adjustments
                netPay = Math.Round(grossPay + totalAllowances - totalDeductions, 2, MidpointRounding.AwayFromZero);
            }

            // Apply net pay floor rule: final fallback for statutory deductions excess
            if (netPay < 0)
            {
                trace.AppendLine($"[Warning] Net pay floor rule triggered. Statutory deductions ({totalDeductions:C}) exceed gross + allowances ({grossPay + totalAllowances:C}). Adjusting Net Pay to $0.00.");
                netPay = 0;
            }

            employeeResults.Add(new CalculatedEmployeeResult
            {
                EmployeeId = emp.EmployeeId,
                EmployeeName = emp.FullName,
                Tin = emp.Tin,
                FnpfNumber = emp.FnpfNumber,
                ResidencyStatus = emp.ResidencyStatus,
                Department = emp.Department,
                BaseSalary = basicSalary,
                GrossPay = grossPay,
                TotalAllowances = totalAllowances,
                TotalDeductions = totalDeductions,
                NetPay = netPay,
                PayeTax = payeTax,
                FnpfEmployeeContribution = fnpfEmployee,
                FnpfEmployerContribution = fnpfEmployer,
                TaxVersionUsed = context.TaxVersion,
                TraceText = trace.ToString(),
                LineItems = calculatedLines
            });
        }

        // HASHING RULE: PayrollSnapshotHasher is the ONLY valid source for snapshot hash generation.
        string snapshotHash = PayrollSnapshotHasher.GenerateHash(context.Employees, context.TaxVersion, context.Components);

        return new CalculationResultSnapshot
        {
            PayrollRunId = context.PayrollRunId,
            SnapshotHash = snapshotHash,
            CalculationRequestId = context.CalculationRequestId,
            Employees = employeeResults
        };
    }
}
