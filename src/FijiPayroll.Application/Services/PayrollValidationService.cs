using System;
using System.Collections.Generic;
using System.Text;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service validating the input execution context before triggering the payroll run calculation.
/// Enforces statutory completeness constraints (TIN, FNPF numbers, non-negative salaries, etc.).
/// </summary>
public sealed class PayrollValidationService
{
    /// <summary>
    /// Validates the execution context. Throws an exception or returns a list of validation errors.
    /// </summary>
    public void Validate(PayrollExecutionContext context)
    {
        var errors = new List<string>();

        if (context.StartDate >= context.EndDate)
        {
            errors.Add($"Run period dates are invalid: StartDate '{context.StartDate:yyyy-MM-dd}' must be before EndDate '{context.EndDate:yyyy-MM-dd}'.");
        }

        if (context.TaxRules == null || context.TaxRules.Count == 0)
        {
            errors.Add($"Statutory configuration error: No active tax rules snapshot loaded for tax version '{context.TaxVersion}'.");
        }

        if (context.Employees == null || context.Employees.Count == 0)
        {
            errors.Add("Calculation error: No employee snapshots loaded for the payroll run.");
        }
        else
        {
            foreach (var emp in context.Employees)
            {
                if (emp.BaseSalary < 0)
                {
                    errors.Add($"Employee '{emp.FullName}' (ID: {emp.EmployeeId}) has a negative base salary: {emp.BaseSalary}.");
                }

                if (string.IsNullOrWhiteSpace(emp.Tin))
                {
                    // Warning or error? Standard rules indicate warning but let's register as validation item.
                    // We can log or raise it. Let's make it a validation warning/error for strict processing.
                    errors.Add($"FRCS rule violation: Employee '{emp.FullName}' (ID: {emp.EmployeeId}) is missing a Tax Identification Number (TIN).");
                }

                if (!emp.IsFnpfExempt && string.IsNullOrWhiteSpace(emp.FnpfNumber))
                {
                    errors.Add($"FNPF rule violation: Employee '{emp.FullName}' (ID: {emp.EmployeeId}) is not FNPF-exempt but lacks an FNPF registration number.");
                }
            }
        }

        if (errors.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Payroll calculation validation failed with the following errors:");
            foreach (var error in errors)
            {
                sb.AppendLine($"- {error}");
            }
            throw new InvalidOperationException(sb.ToString());
        }
    }
}
