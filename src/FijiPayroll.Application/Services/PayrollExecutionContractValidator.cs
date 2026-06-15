using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Entities.Company;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Hardened execution validator validating execution contracts before engine calculation starts.
/// </summary>
public static class PayrollExecutionContractValidator
{
    /// <summary>
    /// Validates the structure and values of the context to ensure mathematical and audit determinism.
    /// </summary>
    public static void Validate(PayrollExecutionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context), "Execution context cannot be null.");
        }

        // 1. Employee ordering check
        for (int i = 0; i < context.Employees.Count - 1; i++)
        {
            if (context.Employees[i].EmployeeId >= context.Employees[i + 1].EmployeeId)
            {
                throw new InvalidOperationException("CONTRACT_VIOLATION: Employee dataset is not sorted by EmployeeId in strictly ascending order.");
            }
        }

        // 2. Duplicate EmployeeId check
        var duplicateIds = context.Employees
            .GroupBy(e => e.EmployeeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw new InvalidOperationException($"CONTRACT_VIOLATION: Duplicate EmployeeIds detected: {string.Join(", ", duplicateIds)}");
        }

        // 3. Currency/decimal integrity intact
        foreach (var emp in context.Employees)
        {
            if (emp.BaseSalary < 0)
            {
                throw new InvalidOperationException($"CONTRACT_VIOLATION: Employee {emp.EmployeeId} has negative BaseSalary: {emp.BaseSalary}");
            }
        }

        // 4. Missing required component codes
        var requiredCodes = new[] { "BASIC", "PAYE" };
        foreach (var code in requiredCodes)
        {
            if (!context.Components.Any(c => c.ComponentCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"CONTRACT_VIOLATION: Required component code '{code}' is missing from the active configurations.");
            }
        }

        // Check FNPF components (either standard or alternative code representations)
        bool hasFnpfEe = context.Components.Any(c => c.ComponentCode.Equals("FNPF_EE", StringComparison.OrdinalIgnoreCase) || c.ComponentCode.Equals("FNPF-EMP", StringComparison.OrdinalIgnoreCase));
        bool hasFnpfEr = context.Components.Any(c => c.ComponentCode.Equals("FNPF_ER", StringComparison.OrdinalIgnoreCase) || c.ComponentCode.Equals("FNPF-EMPLR", StringComparison.OrdinalIgnoreCase));
        if (!hasFnpfEe || !hasFnpfEr)
        {
            throw new InvalidOperationException("CONTRACT_VIOLATION: Required FNPF component codes (employee/employer) are missing.");
        }

        // 5. Tax brackets validation (no null gaps)
        if (context.TaxRules == null || !context.TaxRules.Any())
        {
            throw new InvalidOperationException("CONTRACT_VIOLATION: Tax bracket rule set is empty.");
        }

        var residentBrackets = context.TaxRules
            .Where(b => b.ResidencyStatus.Equals("Resident", StringComparison.OrdinalIgnoreCase) && b.IsActive)
            .OrderBy(b => b.LowerLimit)
            .ToList();

        if (residentBrackets.Count == 0)
        {
            throw new InvalidOperationException("CONTRACT_VIOLATION: No active Resident tax brackets found.");
        }

        // Validate consecutive resident bracket bounds
        if (residentBrackets[0].LowerLimit != 0m)
        {
            throw new InvalidOperationException($"CONTRACT_VIOLATION: Resident tax brackets must start at lower limit 0. Current start: {residentBrackets[0].LowerLimit}");
        }

        for (int i = 0; i < residentBrackets.Count - 1; i++)
        {
            if (residentBrackets[i].UpperLimit != residentBrackets[i + 1].LowerLimit)
            {
                throw new InvalidOperationException($"CONTRACT_VIOLATION: Gap detected in Resident tax brackets between {residentBrackets[i].UpperLimit} and {residentBrackets[i + 1].LowerLimit}");
            }
        }

        var nonResidentBrackets = context.TaxRules
            .Where(b => b.ResidencyStatus.Equals("NonResident", StringComparison.OrdinalIgnoreCase) && b.IsActive)
            .OrderBy(b => b.LowerLimit)
            .ToList();

        if (nonResidentBrackets.Count == 0)
        {
            throw new InvalidOperationException("CONTRACT_VIOLATION: No active NonResident tax brackets found.");
        }

        if (nonResidentBrackets[0].LowerLimit != 0m)
        {
            throw new InvalidOperationException($"CONTRACT_VIOLATION: NonResident tax brackets must start at lower limit 0. Current start: {nonResidentBrackets[0].LowerLimit}");
        }

        for (int i = 0; i < nonResidentBrackets.Count - 1; i++)
        {
            if (nonResidentBrackets[i].UpperLimit != nonResidentBrackets[i + 1].LowerLimit)
            {
                throw new InvalidOperationException($"CONTRACT_VIOLATION: Gap detected in NonResident tax brackets between {nonResidentBrackets[i].UpperLimit} and {nonResidentBrackets[i + 1].LowerLimit}");
            }
        }
    }
}
