using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity tracking applied reference data seed versions per tenant.
/// </summary>
public sealed class CompanySeedVersion : SoftDeleteEntity
{
    private CompanySeedVersion() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the semantic seed version string (e.g. "1.0").</summary>
    public string SeedVersion { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the seed was applied.</summary>
    public DateTime AppliedUtc { get; private set; }

    /// <summary>Gets the seed application summary description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the category of the seed applied.</summary>
    public SeedCategory SeedCategory { get; private set; }

    /// <summary>Factory method to create a new CompanySeedVersion.</summary>
    public static CompanySeedVersion Create(
        int companyId,
        string seedVersion,
        string description,
        SeedCategory category)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(seedVersion))
            throw new ArgumentException("Seed version cannot be empty.", nameof(seedVersion));

        return new CompanySeedVersion
        {
            CompanyId = companyId,
            SeedVersion = seedVersion,
            AppliedUtc = DateTime.UtcNow,
            Description = description ?? string.Empty,
            SeedCategory = category
        };
    }
}
