using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Idempotent database seeder for FRCS tax brackets (Resident & Non-Resident, all pay frequencies).
/// </summary>
public sealed class TaxBracketSeeder
{
    private readonly ApplicationDbContext _context;

    public TaxBracketSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seeds the tax brackets if none exist.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.TaxBrackets.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        var brackets = new List<TaxBracket>();
        var frequencies = new[]
        {
            PayrollFrequencyType.Weekly,
            PayrollFrequencyType.Fortnightly,
            PayrollFrequencyType.BiMonthly,
            PayrollFrequencyType.Monthly
        };

        string version = "2025-2026";
        DateTime effectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var freq in frequencies)
        {
            // ─── RESIDENT BRACKETS (Annualized Limits) ──────────────────────────────
            
            // Band 1: 0 - 30,000 (0%)
            brackets.Add(TaxBracket.Create(
                taxVersion: version,
                residencyStatus: "Resident",
                frequency: freq,
                lowerLimit: 0m,
                upperLimit: 30000m,
                taxRate: 0.00m,
                fixedTaxAmount: 0m,
                isActive: true,
                effectiveDate: effectiveDate
            ));

            // Band 2: 30,000 - 50,000 (18% on excess over 30,000)
            brackets.Add(TaxBracket.Create(
                taxVersion: version,
                residencyStatus: "Resident",
                frequency: freq,
                lowerLimit: 30000m,
                upperLimit: 50000m,
                taxRate: 0.18m,
                fixedTaxAmount: 0m,
                isActive: true,
                effectiveDate: effectiveDate
            ));

            // Band 3: 50,000 - 270,000 (20% on excess over 50,000 + 3,600)
            brackets.Add(TaxBracket.Create(
                taxVersion: version,
                residencyStatus: "Resident",
                frequency: freq,
                lowerLimit: 50000m,
                upperLimit: 270000m,
                taxRate: 0.20m,
                fixedTaxAmount: 3600m,
                isActive: true,
                effectiveDate: effectiveDate
            ));

            // Band 4: 270,000 - 300,000 (20% + 47,600)
            brackets.Add(TaxBracket.Create(
                taxVersion: version,
                residencyStatus: "Resident",
                frequency: freq,
                lowerLimit: 270000m,
                upperLimit: 300000m,
                taxRate: 0.20m,
                fixedTaxAmount: 47600m,
                isActive: true,
                effectiveDate: effectiveDate
            ));

            // Band 5: 300,000+ (20% + 53,600)
            brackets.Add(TaxBracket.Create(
                taxVersion: version,
                residencyStatus: "Resident",
                frequency: freq,
                lowerLimit: 300000m,
                upperLimit: 999999999m,
                taxRate: 0.20m,
                fixedTaxAmount: 53600m,
                isActive: true,
                effectiveDate: effectiveDate
            ));

            // ─── NON-RESIDENT BRACKETS (Flat 20% on all income) ─────────────────────
            
            brackets.Add(TaxBracket.Create(
                taxVersion: version,
                residencyStatus: "NonResident",
                frequency: freq,
                lowerLimit: 0m,
                upperLimit: 999999999m,
                taxRate: 0.20m,
                fixedTaxAmount: 0m,
                isActive: true,
                effectiveDate: effectiveDate
            ));
        }

        // Add auditor stamped properties directly
        foreach (var b in brackets)
        {
            b.CreatedBy = "system-seeder";
            b.CreatedAt = DateTime.UtcNow;
        }

        await _context.TaxBrackets.AddRangeAsync(brackets, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
