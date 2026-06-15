using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Hashing utility that is the ONLY valid source for snapshot hash generation in the system.
/// Generates deterministic SHA-256 hash using sorted datasets to ensure audit compliance.
/// </summary>
public static class PayrollSnapshotHasher
{
    private const string HashVersion = "v1";

    /// <summary>
    /// Computes a deterministic hash of the payroll execution input configurations.
    /// </summary>
    public static string GenerateHash(
        IEnumerable<EmployeeSnapshot> employees,
        string taxVersion,
        IEnumerable<PayrollComponentSnapshot> components)
    {
        // 1. Order employees by EmployeeId (DB level sorting matches this, but we reinforce it here)
        var sortedEmployees = employees.OrderBy(e => e.EmployeeId).ToList();

        // 2. Normalize tax version (trimmed, lowercase)
        var normalizedTax = (taxVersion ?? string.Empty).Trim().ToLowerInvariant();

        // 3. Order components by ComponentCode
        var sortedComponents = components.OrderBy(c => c.ComponentCode).ToList();

        // 4. Build components of normalized string representation
        var empBuilder = new StringBuilder();
        foreach (var emp in sortedEmployees)
        {
            string residency = (emp.ResidencyStatus ?? string.Empty).Trim().ToLowerInvariant();
            string isFnpfExempt = emp.IsFnpfExempt.ToString().ToLowerInvariant();
            string isTaxExempt = emp.IsTaxExempt.ToString().ToLowerInvariant();

            empBuilder.Append(string.Format(CultureInfo.InvariantCulture, "emp:{0}:{1}:{2}:{3}:{4};",
                emp.EmployeeId,
                NormalizeDecimal(emp.BaseSalary),
                residency,
                isFnpfExempt,
                isTaxExempt));

            foreach (var ov in emp.ComponentOverrides.OrderBy(o => o.ComponentCode))
            {
                string code = (ov.ComponentCode ?? string.Empty).Trim().ToUpperInvariant();
                empBuilder.Append(string.Format(CultureInfo.InvariantCulture, "ov:{0}:{1};",
                    code,
                    NormalizeDecimal(ov.Value)));
            }
        }

        var compBuilder = new StringBuilder();
        foreach (var comp in sortedComponents)
        {
            string code = (comp.ComponentCode ?? string.Empty).Trim().ToUpperInvariant();
            string formula = (comp.Formula ?? string.Empty).Trim();
            string isTaxable = comp.IsTaxable.ToString().ToLowerInvariant();
            string isFnpf = comp.IsFnpfApplicable.ToString().ToLowerInvariant();

            compBuilder.Append(string.Format(CultureInfo.InvariantCulture, "comp:{0}:{1}:{2}:{3}:{4}:{5}:{6};",
                code,
                comp.ComponentType,
                comp.CalculationMethod,
                NormalizeDecimal(comp.CalculationValue ?? 0m),
                formula,
                isTaxable,
                isFnpf));
        }

        // Combine into the required format: "[HashVersion]|EmployeeDataSorted|TaxVersionNormalized|ComponentSorted"
        string hashInput = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}",
            HashVersion,
            empBuilder,
            normalizedTax,
            compBuilder);

        // 5. Generate SHA-256
        byte[] inputBytes = Encoding.UTF8.GetBytes(hashInput);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        
        // Return hexadecimal representation (lower case)
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string NormalizeDecimal(decimal d)
    {
        // Format to general decimal notation using invariant culture
        string formatted = d.ToString("G29", CultureInfo.InvariantCulture);
        if (formatted.Contains('.'))
        {
            formatted = formatted.TrimEnd('0').TrimEnd('.');
        }
        return formatted;
    }
}
