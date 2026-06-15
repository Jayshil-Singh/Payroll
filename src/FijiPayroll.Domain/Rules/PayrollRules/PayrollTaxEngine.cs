using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.Domain.Rules.PayrollRules;

/// <summary>
/// Pure stateless engine calculating PAYE progressive tax using configuration-driven tax brackets.
/// </summary>
public static class PayrollTaxEngine
{
    /// <summary>
    /// Calculates period PAYE tax by annualising gross taxable income, applying progressive brackets, and de-annualising.
    /// </summary>
    public static decimal CalculatePaye(
        decimal periodTaxableGross,
        decimal periodFnpfEmployeeContribution,
        PayrollFrequency frequency,
        string residencyStatus,
        string taxVersion,
        IEnumerable<TaxBracket> taxBrackets)
    {
        // 1. Get periods count per year
        int periodsPerYear = frequency switch
        {
            PayrollFrequency.Weekly => 52,
            PayrollFrequency.Fortnightly => 26,
            PayrollFrequency.BiMonthly => 24,
            PayrollFrequency.Monthly => 12,
            _ => throw new ArgumentException($"Invalid frequency '{frequency}'")
        };

        // 2. Annualise taxable gross income
        decimal annualGross = periodTaxableGross * periodsPerYear;

        // 3. Subtract FNPF employee contribution (since it is tax-deductible in Fiji)
        decimal annualFnpfDeduction = periodFnpfEmployeeContribution * periodsPerYear;
        decimal taxableAnnualIncome = Math.Max(0, annualGross - annualFnpfDeduction);

        // 4. Filter and validate brackets
        var matchingBrackets = taxBrackets
            .Where(b => b.TaxVersion.Equals(taxVersion, StringComparison.OrdinalIgnoreCase)
                     && b.ResidencyStatus.Equals(residencyStatus, StringComparison.OrdinalIgnoreCase)
                     && b.Frequency == frequency
                     && b.IsActive)
            .OrderBy(b => b.LowerLimit)
            .ToList();

        if (matchingBrackets.Count == 0)
        {
            throw new InvalidOperationException(
                $"TAX_ENGINE_ERROR: No active tax brackets found for Version: '{taxVersion}', Residency: '{residencyStatus}', Frequency: '{frequency}'. NO silent fallbacks permitted.");
        }

        // 5. Find the bracket that contains the taxable annual income
        TaxBracket? matchedBracket = matchingBrackets
            .FirstOrDefault(b => taxableAnnualIncome >= b.LowerLimit && taxableAnnualIncome <= b.UpperLimit);

        if (matchedBracket == null)
        {
            // Fallback: Check if it exceeds the highest bracket
            var lastBracket = matchingBrackets.Last();
            if (taxableAnnualIncome >= lastBracket.LowerLimit)
            {
                matchedBracket = lastBracket;
            }
        }

        if (matchedBracket == null)
        {
            throw new InvalidOperationException(
                $"TAX_ENGINE_ERROR: Taxable annual income {taxableAnnualIncome:C} does not fall within any configured tax brackets.");
        }

        // 6. Compute annual PAYE
        decimal excessIncome = taxableAnnualIncome - matchedBracket.LowerLimit;
        decimal annualPaye = matchedBracket.FixedTaxAmount + (excessIncome * matchedBracket.TaxRate);
        annualPaye = Math.Max(0, annualPaye);

        // 7. De-annualise
        decimal periodPaye = annualPaye / periodsPerYear;

        // Fiji Rounding rule: Round to 2 decimal places, half up.
        return Math.Round(periodPaye, 2, MidpointRounding.AwayFromZero);
    }
}
