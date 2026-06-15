using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain aggregate representing a business tenant (Company) with profile, currency, timezone, and security configurations.
/// </summary>
public sealed class Company : ArchivableEntity
{
    private string _legalName = string.Empty;
    private string _securityIsolatorKey = string.Empty;
    private string _timeZone = "Fiji Standard Time";
    private string _defaultCurrency = "FJD";

    private Company() { }

    /// <summary>Gets the legal registered company name.</summary>
    public string LegalName
    {
        get => _legalName;
        private set => _legalName = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the unique cryptographic key used for database row-level security isolation.</summary>
    public string SecurityIsolatorKey
    {
        get => _securityIsolatorKey;
        private set => _securityIsolatorKey = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the local time zone code (defaults to Fiji Standard Time).</summary>
    public string TimeZone
    {
        get => _timeZone;
        private set => _timeZone = value ?? "Fiji Standard Time";
    }

    /// <summary>Gets the default currency ISO code (defaults to FJD).</summary>
    public string DefaultCurrency
    {
        get => _defaultCurrency;
        private set => _defaultCurrency = value ?? "FJD";
    }

    /// <summary>Factory method to create a new Company tenant.</summary>
    public static Company Create(
        string legalName,
        string securityIsolatorKey,
        string timeZone = "Fiji Standard Time",
        string defaultCurrency = "FJD")
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new ArgumentException("Legal name is required.", nameof(legalName));
        if (string.IsNullOrWhiteSpace(securityIsolatorKey))
            throw new ArgumentException("Security isolator key is required.", nameof(securityIsolatorKey));

        return new Company
        {
            LegalName = legalName,
            SecurityIsolatorKey = securityIsolatorKey,
            TimeZone = timeZone,
            DefaultCurrency = defaultCurrency
        };
    }

    /// <summary>Updates profile parameters of the company tenant.</summary>
    public void UpdateProfile(string legalName, string timeZone, string defaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new ArgumentException("Legal name is required.", nameof(legalName));

        LegalName = legalName;
        TimeZone = timeZone;
        DefaultCurrency = defaultCurrency;
    }
}
