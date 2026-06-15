using FijiPayroll.Domain.Enumerations;
using System;
using System.Globalization;

namespace FijiPayroll.Domain.Rules.PayrollRules;

/// <summary>
/// Pure stateless engine evaluating allowance amounts based on component configuration.
/// </summary>
public static class PayrollAllowanceEngine
{
    /// <summary>
    /// Computes the allowance value for the current period based on calculation method.
    /// </summary>
    public static decimal CalculateAllowance(
        CalculationMethod method,
        decimal? value,
        string? formula,
        decimal baseSalary,
        decimal hoursWorked,
        decimal overtimeHours,
        decimal? manualOverrideValue)
    {
        switch (method)
        {
            case CalculationMethod.Fixed:
                return value ?? 0m;

            case CalculationMethod.Percentage:
                if (value == null) return 0m;
                // Percentage of base salary (e.g. value = 10 means 10%)
                decimal percentage = value.Value / 100m;
                return Math.Round(baseSalary * percentage, 2, MidpointRounding.AwayFromZero);

            case CalculationMethod.Manual:
                return manualOverrideValue ?? 0m;

            case CalculationMethod.Formula:
                if (string.IsNullOrWhiteSpace(formula))
                {
                    return 0m;
                }
                return EvaluateFormula(formula, baseSalary, hoursWorked, overtimeHours);

            default:
                return 0m;
        }
    }

    private static decimal EvaluateFormula(string formula, decimal baseSalary, decimal hoursWorked, decimal overtimeHours)
    {
        try
        {
            // Replace tokens
            string expr = formula
                .Replace("{BaseSalary}", baseSalary.ToString(CultureInfo.InvariantCulture))
                .Replace("{GrossPay}", baseSalary.ToString(CultureInfo.InvariantCulture)) // fallback GrossPay to BaseSalary in pure context
                .Replace("{HoursWorked}", hoursWorked.ToString(CultureInfo.InvariantCulture))
                .Replace("{OvertimeHours}", overtimeHours.ToString(CultureInfo.InvariantCulture));

            // Run simple evaluation (e.g. using a basic recursive descent parsing or DataTable as in-memory expression calculator)
            // Let's implement a simple parser for basic multiplications/additions to be safe and cross-platform
            return EvaluateMathExpression(expr);
        }
        catch
        {
            // If evaluation fails, return 0 rather than crashing, or throw a deterministic error if strictly required.
            // Let's return 0 for safe fallback on bad formula input.
            return 0m;
        }
    }

    private static decimal EvaluateMathExpression(string expression)
    {
        // For basic formulas, e.g. "BaseSalary * 0.15" or "HoursWorked * 15"
        // Let's use DataTable's built-in expression parser which is lightweight and standard in .NET
        using (var table = new System.Data.DataTable())
        {
            table.Columns.Add("expression", typeof(string), expression);
            var row = table.NewRow();
            table.Rows.Add(row);
            var resultStr = row["expression"]?.ToString();
            if (decimal.TryParse(resultStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
            {
                return Math.Round(val, 2, MidpointRounding.AwayFromZero);
            }
        }
        return 0m;
    }
}
