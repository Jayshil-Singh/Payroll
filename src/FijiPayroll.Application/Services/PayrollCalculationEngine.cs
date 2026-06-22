using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using FijiPayroll.Domain.Exceptions;
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
        // ORDERING RULE: Calculations must use ordered list by EmployeeId.
        // We enforce sorting by EmployeeId here as well to guarantee determinism.
        var sortedEmployees = context.Employees.OrderBy(e => e.EmployeeId).ToList();

        List<CalculatedEmployeeResult> employeeResults;
        try
        {
            employeeResults = sortedEmployees.AsParallel().AsOrdered().Select(emp =>
        {
            var trace = new StringBuilder();
            try
            {
                trace.AppendLine($"[Trace] Starting payroll calculation for Employee ID: {emp.EmployeeId}, Name: {emp.FullName}");
                trace.AppendLine($"[Trace] Base Salary: {emp.BaseSalary:C}");
                trace.AppendLine($"[Trace] Frequency: {context.Frequency}");
                trace.AppendLine($"[Trace] Residency: {emp.ResidencyStatus}");

                decimal standardWorkingDays = GetStandardWorkingDays(context.Frequency);
                decimal dailyRate = emp.BaseSalary / standardWorkingDays;

                decimal unpaidLeaveReduction = 0;
                if (emp.UnpaidLeaveDays > 0)
                {
                    unpaidLeaveReduction = Math.Round(dailyRate * emp.UnpaidLeaveDays, 2, MidpointRounding.AwayFromZero);
                }

                decimal basicSalary = Math.Round(emp.BaseSalary, 2, MidpointRounding.AwayFromZero);
                decimal totalEarnings = basicSalary - unpaidLeaveReduction;
                decimal totalAllowances = 0;
                var employeeAuditEvents = new List<PayrollAuditEvent>();

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

                if (unpaidLeaveReduction > 0)
                {
                    var unpaidComp = context.Components.FirstOrDefault(c => 
                        c.ComponentCode.Equals("LEAVE_UNPAID", StringComparison.OrdinalIgnoreCase) ||
                        c.ComponentCode.Equals("LVE_UNP", StringComparison.OrdinalIgnoreCase));
                    
                    calculatedLines.Add(new CalculatedLineItemResult
                    {
                        ComponentId = unpaidComp?.Id ?? 999901,
                        ComponentCode = unpaidComp?.ComponentCode ?? "LEAVE_UNPAID",
                        ComponentName = unpaidComp?.ComponentName ?? "Unpaid Leave Deduction",
                        ComponentType = ComponentType.Earning,
                        Amount = -unpaidLeaveReduction,
                        IsTaxable = true,
                        AffectsFnpf = true,
                        EmployerContributionFlag = false,
                        ReferenceComponentId = unpaidComp?.Id ?? 999901
                    });
                    trace.AppendLine($"[Trace] Unpaid Leave: {emp.UnpaidLeaveDays} days. Reduction: -{unpaidLeaveReduction:C}");
                }

                decimal leaveLoadingAmount = 0;
                if (emp.LeaveLoadingDays > 0)
                {
                    leaveLoadingAmount = Math.Round(dailyRate * emp.LeaveLoadingDays * 0.25m, 2, MidpointRounding.AwayFromZero);
                    
                    var loadingComp = context.Components.FirstOrDefault(c => 
                        c.ComponentCode.Equals("LEAVE_LOADING", StringComparison.OrdinalIgnoreCase) ||
                        c.ComponentCode.Equals("LVE_LOD", StringComparison.OrdinalIgnoreCase) ||
                        c.ComponentCode.Equals("LL", StringComparison.OrdinalIgnoreCase));
                    
                    calculatedLines.Add(new CalculatedLineItemResult
                    {
                        ComponentId = loadingComp?.Id ?? 999902,
                        ComponentCode = loadingComp?.ComponentCode ?? "LEAVE_LOADING",
                        ComponentName = loadingComp?.ComponentName ?? "Leave Loading (25%)",
                        ComponentType = ComponentType.Earning,
                        Amount = leaveLoadingAmount,
                        IsTaxable = true,
                        AffectsFnpf = true,
                        EmployerContributionFlag = false,
                        ReferenceComponentId = loadingComp?.Id ?? 999902
                    });
                    totalEarnings += leaveLoadingAmount;
                    trace.AppendLine($"[Trace] Leave Loading: {emp.LeaveLoadingDays} days. Amount: {leaveLoadingAmount:C}");
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

                    decimal amount = Math.Round(PayrollAllowanceEngine.CalculateAllowance(
                        comp.CalculationMethod,
                        comp.CalculationValue,
                        comp.Formula,
                        basicSalary,
                        emp.HoursWorked,
                        emp.OvertimeHours,
                        overrideVal
                    ), 2, MidpointRounding.AwayFromZero);

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
                decimal fnpfEmployee = PayrollDeductionEngine.CalculateEmployeeFnpf(
                    fnpfApplicableGross, emp.IsFnpfExempt, context.FnpfEmployeeRate);
                decimal fnpfEmployer = PayrollDeductionEngine.CalculateEmployerFnpf(
                    fnpfApplicableGross, emp.IsFnpfExempt, context.FnpfEmployerRate);

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
                    trace.AppendLine($"[Trace] FNPF Employee contribution: {fnpfEmployee:C}");
                }

                var fnperComp = context.Components.FirstOrDefault(c => 
                    c.ComponentCode.Equals("FNPF_ER", StringComparison.OrdinalIgnoreCase) || 
                    c.ComponentCode.Equals("FNPF-ER", StringComparison.OrdinalIgnoreCase) || 
                    c.ComponentCode.Equals(FijiTaxConstants.FnpfEmployerComponentCode, StringComparison.OrdinalIgnoreCase));
                if (fnperComp != null && fnpfEmployer > 0)
                {
                    calculatedLines.Add(new CalculatedLineItemResult
                    {
                        ComponentId = fnperComp.Id,
                        ComponentCode = fnperComp.ComponentCode,
                        ComponentName = fnperComp.ComponentName,
                        ComponentType = fnperComp.ComponentType,
                        Amount = fnpfEmployer,
                        IsTaxable = fnperComp.IsTaxable,
                        AffectsFnpf = fnperComp.IsFnpfApplicable,
                        EmployerContributionFlag = true,
                        ReferenceComponentId = fnperComp.Id
                    });
                    trace.AppendLine($"[Trace] FNPF Employer contribution: {fnpfEmployer:C}");
                }

                // Calculate taxable gross
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

                // Calculate PAYE income tax
                decimal payeTax = 0;
                if (!emp.IsTaxExempt)
                {
                    var mappedBrackets = context.TaxRules.ToList();

                    payeTax = Math.Round(PayrollTaxEngine.CalculatePaye(
                        periodTaxableGross,
                        fnpfEmployee,
                        context.Frequency,
                        emp.ResidencyStatus,
                        context.TaxVersion,
                        mappedBrackets
                    ), 2, MidpointRounding.AwayFromZero);
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
                    trace.AppendLine($"[Trace] PAYE tax calculated: {payeTax:C}");
                }

                // Group statutory deductions
                decimal statutoryDeductions = Math.Round(fnpfEmployee + payeTax, 2, MidpointRounding.AwayFromZero);
                decimal totalDeductions = 0;

                // Process non-statutory/voluntary deductions
                var deductionComponents = context.Components
                    .Where(c => c.ComponentType == ComponentType.Deduction 
                              && !c.ComponentCode.Equals("FNPF_EE", StringComparison.OrdinalIgnoreCase)
                              && !c.ComponentCode.Equals("FNPF-EMP", StringComparison.OrdinalIgnoreCase)
                              && !c.ComponentCode.Equals(FijiTaxConstants.FnpfEmployeeComponentCode, StringComparison.OrdinalIgnoreCase)
                              && !c.ComponentCode.Equals("PAYE", StringComparison.OrdinalIgnoreCase)
                              && !c.ComponentCode.Equals(FijiTaxConstants.PayeComponentCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var voluntaryLines = new List<CalculatedLineItemResult>();

                foreach (var comp in deductionComponents)
                {
                    var manualOverride = emp.ComponentOverrides.FirstOrDefault(o => o.ComponentCode.Equals(comp.ComponentCode, StringComparison.OrdinalIgnoreCase));
                    decimal? overrideVal = manualOverride?.Value;

                    decimal amount = Math.Round(PayrollAllowanceEngine.CalculateAllowance(
                        comp.CalculationMethod,
                        comp.CalculationValue,
                        comp.Formula,
                        basicSalary,
                        emp.HoursWorked,
                        emp.OvertimeHours,
                        overrideVal
                    ), 2, MidpointRounding.AwayFromZero);

                    if (amount != 0)
                    {
                        var voluntaryLine = new CalculatedLineItemResult
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
                        };
                        calculatedLines.Add(voluntaryLine);
                        voluntaryLines.Add(voluntaryLine);
                        trace.AppendLine($"[Trace] Calculated Deduction '{comp.ComponentName}' ({comp.ComponentCode}): {amount:C}");
                    }
                }

                // Compute gross pay and allowances
                decimal grossPay = Math.Round(totalEarnings, 2, MidpointRounding.AwayFromZero);
                decimal totalAllowancesRounded = Math.Round(totalAllowances, 2, MidpointRounding.AwayFromZero);

                // Compute net pay before voluntary deductions
                decimal netPayBeforeVoluntary = Math.Round(grossPay + totalAllowancesRounded - statutoryDeductions, 2, MidpointRounding.AwayFromZero);
                decimal totalVoluntaryDeductions = Math.Round(voluntaryLines.Sum(l => -l.Amount), 2, MidpointRounding.AwayFromZero);

                decimal netPay = Math.Round(netPayBeforeVoluntary - totalVoluntaryDeductions, 2, MidpointRounding.AwayFromZero);
                trace.AppendLine($"[Trace] Calculated Net Pay raw: {netPay:C}");

                // Safety check: Statutory deductions alone cause negative net pay
                if (netPayBeforeVoluntary < 0)
                {
                    netPay = 0.00m;
                    
                    foreach (var line in voluntaryLines)
                    {
                        line.Amount = 0.00m;
                    }

                    var warnMsg = $"Statutory deductions ({statutoryDeductions:C}) exceed gross plus allowances ({grossPay + totalAllowancesRounded:C}). Net pay forced to 0.00.";
                    trace.AppendLine($"[Warning] {warnMsg}");
                    var auditEv = new PayrollAuditEvent(
                        "STATUTORY_DEDUCTIONS_EXCEED_GROSS",
                        "Warning",
                        warnMsg,
                        emp.FullName
                    );
                    employeeAuditEvents.Add(auditEv);

                    totalDeductions = statutoryDeductions;
                }
                // Insufficient net pay for voluntary deductions
                else if (netPay < 0)
                {
                    var excMsg = $"Insufficient net pay for employee '{emp.FullName}' to cover voluntary deductions. Net pay would be {netPay:C}.";
                    trace.AppendLine($"[Error] {excMsg}");
                    var exceptionEv = new PayrollAuditEvent(
                        "INSUFFICIENT_NET_PAY_FOR_VOLUNTARY_DEDUCTIONS",
                        "Error",
                        excMsg,
                        emp.FullName
                    );
                    employeeAuditEvents.Add(exceptionEv);

                    if (context.VoluntaryDeductionPolicy == VoluntaryDeductionPolicy.BlockPayroll)
                    {
                        throw new PayrollException("INSUFFICIENT_NET_PAY_FOR_VOLUNTARY_DEDUCTIONS", excMsg);
                    }
                    else if (context.VoluntaryDeductionPolicy == VoluntaryDeductionPolicy.CarryForwardRemainder)
                    {
                        decimal availableForVoluntary = netPayBeforeVoluntary;

                        foreach (var line in voluntaryLines)
                        {
                            decimal required = -line.Amount;
                            if (availableForVoluntary >= required)
                            {
                                availableForVoluntary -= required;
                            }
                            else if (availableForVoluntary > 0)
                            {
                                decimal partial = availableForVoluntary;
                                decimal remainder = required - partial;
                                line.Amount = -partial;
                                availableForVoluntary = 0;

                                var carryMsg = $"Voluntary deduction '{line.ComponentName}' partially applied (Reduced by {remainder:C} to {partial:C}). Remainder of {remainder:C} carried forward.";
                                trace.AppendLine($"[Audit] {carryMsg}");
                                var carryEv = new PayrollAuditEvent(
                                    "VOLUNTARY_DEDUCTION_CARRIED_FORWARD",
                                    "Audit",
                                    carryMsg,
                                    emp.FullName
                                );
                                employeeAuditEvents.Add(carryEv);
                            }
                            else
                            {
                                decimal remainder = required;
                                line.Amount = 0.00m;

                                var carryMsg = $"Voluntary deduction '{line.ComponentName}' not applied (Remainder: {remainder:C}). Remainder of {remainder:C} carried forward.";
                                trace.AppendLine($"[Audit] {carryMsg}");
                                var carryEv = new PayrollAuditEvent(
                                    "VOLUNTARY_DEDUCTION_CARRIED_FORWARD",
                                    "Audit",
                                    carryMsg,
                                    emp.FullName
                                );
                                employeeAuditEvents.Add(carryEv);
                            }
                        }

                        // Recalculate totals
                        totalVoluntaryDeductions = Math.Round(voluntaryLines.Sum(l => -l.Amount), 2, MidpointRounding.AwayFromZero);
                        totalDeductions = Math.Round(statutoryDeductions + totalVoluntaryDeductions, 2, MidpointRounding.AwayFromZero);
                        netPay = Math.Round(grossPay + totalAllowancesRounded - totalDeductions, 2, MidpointRounding.AwayFromZero);
                    }
                    else if (context.VoluntaryDeductionPolicy == VoluntaryDeductionPolicy.PartialDeductionWithAuditFlag)
                    {
                        decimal availableForVoluntary = netPayBeforeVoluntary;

                        foreach (var line in voluntaryLines)
                        {
                            decimal required = -line.Amount;
                            if (availableForVoluntary >= required)
                            {
                                availableForVoluntary -= required;
                            }
                            else if (availableForVoluntary > 0)
                            {
                                decimal partial = availableForVoluntary;
                                decimal remainder = required - partial;
                                line.Amount = -partial;
                                availableForVoluntary = 0;

                                var partialMsg = $"Voluntary deduction '{line.ComponentName}' partially applied (Reduced by {remainder:C} to {partial:C}) due to insufficient net pay.";
                                trace.AppendLine($"[Warning] {partialMsg} Audit Flag: INSUFFICIENT_NET_PAY.");
                                var partialEv = new PayrollAuditEvent(
                                    "VOLUNTARY_DEDUCTION_PARTIAL",
                                    "Warning",
                                    partialMsg,
                                    emp.FullName
                                );
                                employeeAuditEvents.Add(partialEv);
                            }
                            else
                            {
                                decimal remainder = required;
                                line.Amount = 0.00m;

                                var partialMsg = $"Voluntary deduction '{line.ComponentName}' not applied (Remainder: {remainder:C}) due to insufficient net pay.";
                                trace.AppendLine($"[Warning] {partialMsg} Audit Flag: INSUFFICIENT_NET_PAY.");
                                var partialEv = new PayrollAuditEvent(
                                    "VOLUNTARY_DEDUCTION_PARTIAL",
                                    "Warning",
                                    partialMsg,
                                    emp.FullName
                                );
                                employeeAuditEvents.Add(partialEv);
                            }
                        }

                        // Recalculate totals
                        totalVoluntaryDeductions = Math.Round(voluntaryLines.Sum(l => -l.Amount), 2, MidpointRounding.AwayFromZero);
                        totalDeductions = Math.Round(statutoryDeductions + totalVoluntaryDeductions, 2, MidpointRounding.AwayFromZero);
                        netPay = Math.Round(grossPay + totalAllowancesRounded - totalDeductions, 2, MidpointRounding.AwayFromZero);
                    }
                }
                else
                {
                    totalDeductions = Math.Round(statutoryDeductions + totalVoluntaryDeductions, 2, MidpointRounding.AwayFromZero);
                }

                // Process Loan Deductions
                decimal totalLoanDeductions = 0m;
                if (emp.ActiveLoans != null && emp.ActiveLoans.Any())
                {
                    foreach (var loanSnap in emp.ActiveLoans)
                    {
                        decimal requestedDeduction = Math.Min(loanSnap.DeductionAmountPerPeriod, loanSnap.RemainingBalance);
                        if (requestedDeduction <= 0) continue;

                        decimal appliedDeduction = 0m;

                        if (netPay >= requestedDeduction)
                        {
                            appliedDeduction = requestedDeduction;
                            netPay = Math.Round(netPay - appliedDeduction, 2, MidpointRounding.AwayFromZero);
                            trace.AppendLine($"[Trace] Applied full loan deduction for Loan ID {loanSnap.LoanId} ({loanSnap.LoanDescription}): {appliedDeduction:C}. New Net Pay: {netPay:C}");
                        }
                        else // netPay < requestedDeduction
                        {
                            if (context.NegativeNetPayPolicy == FijiPayroll.Domain.Entities.Company.NegativeNetPayPolicy.AllowNegativeNetPay)
                            {
                                appliedDeduction = requestedDeduction;
                                netPay = Math.Round(netPay - appliedDeduction, 2, MidpointRounding.AwayFromZero);
                                trace.AppendLine($"[Trace] Applied full loan deduction for Loan ID {loanSnap.LoanId} ({loanSnap.LoanDescription}): {appliedDeduction:C} (AllowNegativeNetPay). New Net Pay: {netPay:C}");
                            }
                            else if (context.NegativeNetPayPolicy == FijiPayroll.Domain.Entities.Company.NegativeNetPayPolicy.BlockDeduction)
                            {
                                var excMsg = $"Calculation aborted: Insufficient net pay for employee '{emp.FullName}' to cover loan deduction of {requestedDeduction:C} for Loan ID {loanSnap.LoanId} ({loanSnap.LoanDescription}). Net pay would be {netPay - requestedDeduction:C}.";
                                trace.AppendLine($"[Error] {excMsg}");
                                var exceptionEv = new PayrollAuditEvent(
                                    "INSUFFICIENT_NET_PAY_FOR_LOAN_DEDUCTION",
                                    "Error",
                                    excMsg,
                                    emp.FullName
                                );
                                employeeAuditEvents.Add(exceptionEv);
                                throw new PayrollException("INSUFFICIENT_NET_PAY_FOR_LOAN_DEDUCTION", excMsg);
                            }
                            else // PartialDeduction
                            {
                                appliedDeduction = Math.Max(0m, netPay);
                                netPay = 0.00m;
                                decimal remainder = requestedDeduction - appliedDeduction;
                                
                                var partialMsg = $"Loan deduction for Loan ID {loanSnap.LoanId} ({loanSnap.LoanDescription}) partially applied (Reduced by {remainder:C} to {appliedDeduction:C}) due to insufficient net pay.";
                                trace.AppendLine($"[Warning] {partialMsg}");
                                var partialEv = new PayrollAuditEvent(
                                    "LOAN_DEDUCTION_PARTIAL",
                                    "Warning",
                                    partialMsg,
                                    emp.FullName
                                );
                                employeeAuditEvents.Add(partialEv);
                            }
                        }

                        if (appliedDeduction > 0)
                        {
                            calculatedLines.Add(new CalculatedLineItemResult
                            {
                                ComponentId = 900000 + loanSnap.LoanId,
                                ComponentCode = $"LOAN_{loanSnap.LoanId}",
                                ComponentName = $"Loan Repayment - {loanSnap.LoanDescription}",
                                ComponentType = ComponentType.Deduction,
                                Amount = -appliedDeduction,
                                IsTaxable = false,
                                AffectsFnpf = false,
                                EmployerContributionFlag = false,
                                ReferenceComponentId = loanSnap.LoanId
                            });
                            totalLoanDeductions += appliedDeduction;
                        }
                    }

                    // Recalculate totalDeductions
                    totalDeductions = Math.Round(totalDeductions + totalLoanDeductions, 2, MidpointRounding.AwayFromZero);
                }

                return new CalculatedEmployeeResult
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeName = emp.FullName,
                    Tin = emp.Tin,
                    FnpfNumber = emp.FnpfNumber,
                    ResidencyStatus = emp.ResidencyStatus,
                    Department = emp.Department,
                    BaseSalary = basicSalary,
                    GrossPay = grossPay,
                    TotalAllowances = totalAllowancesRounded,
                    TotalDeductions = totalDeductions,
                    NetPay = netPay,
                    PayeTax = payeTax,
                    FnpfEmployeeContribution = fnpfEmployee,
                    FnpfEmployerContribution = fnpfEmployer,
                    TaxVersionUsed = context.TaxVersion,
                    TraceText = trace.ToString(),
                    LineItems = calculatedLines,
                    AuditEvents = employeeAuditEvents,
                    IsSuccess = true
                };
            }
            catch (Exception ex) when (ex is not FijiPayroll.Domain.Exceptions.PayrollException)
            {
                return new CalculatedEmployeeResult
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeName = emp.FullName,
                    Tin = emp.Tin,
                    FnpfNumber = emp.FnpfNumber,
                    ResidencyStatus = emp.ResidencyStatus,
                    Department = emp.Department,
                    BaseSalary = emp.BaseSalary,
                    GrossPay = 0m,
                    TotalAllowances = 0m,
                    TotalDeductions = 0m,
                    NetPay = 0m,
                    PayeTax = 0m,
                    FnpfEmployeeContribution = 0m,
                    FnpfEmployerContribution = 0m,
                    TaxVersionUsed = context.TaxVersion,
                    TraceText = trace.ToString() + "\n[Exception] " + ex.ToString(),
                    LineItems = Array.Empty<CalculatedLineItemResult>(),
                    AuditEvents = Array.Empty<PayrollAuditEvent>(),
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ErrorStackTrace = ex.StackTrace
                };
            }
        }).ToList();
        }
        catch (AggregateException aggEx)
        {
            var payrollEx = aggEx.Flatten().InnerExceptions.OfType<FijiPayroll.Domain.Exceptions.PayrollException>().FirstOrDefault();
            if (payrollEx != null)
            {
                throw payrollEx;
            }
            throw;
        }

        var globalAuditEvents = employeeResults.SelectMany(r => r.AuditEvents).ToList();

        // HASHING RULE: PayrollSnapshotHasher is the ONLY valid source for snapshot hash generation.
        string snapshotHash = PayrollSnapshotHasher.GenerateHash(context.Employees, context.TaxVersion, context.Components);

        return new CalculationResultSnapshot
        {
            PayrollRunId = context.PayrollRunId,
            SnapshotHash = snapshotHash,
            CalculationRequestId = context.CalculationRequestId,
            Employees = employeeResults,
            AuditEvents = globalAuditEvents
        };
    }

    private static decimal GetStandardWorkingDays(PayrollFrequencyType frequency)
    {
        return frequency switch
        {
            PayrollFrequencyType.Weekly => 5m,
            PayrollFrequencyType.Fortnightly => 10m,
            PayrollFrequencyType.Monthly => 260m / 12m,
            _ => 10m
        };
    }
}
