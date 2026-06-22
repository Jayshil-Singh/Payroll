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

    /// <summary>Gets or sets the trading name of the company.</summary>
    public string TradingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the 9-digit tax identification number (TIN).</summary>
    public string TIN { get; set; } = string.Empty;

    /// <summary>Gets or sets the FNPF employer registration number.</summary>
    public string FnpfEmployerNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets address line 1.</summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>Gets or sets address line 2.</summary>
    public string AddressLine2 { get; set; } = string.Empty;

    /// <summary>Gets or sets the city of operation.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone contact.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Gets or sets the email contact.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the company website URL.</summary>
    public string Website { get; set; } = string.Empty;

    /// <summary>Gets or sets the country of registration (defaults to Fiji).</summary>
    public string Country { get; set; } = "Fiji";

    /// <summary>Gets or sets the locale format (defaults to en-FJ).</summary>
    public string Locale { get; set; } = "en-FJ";

    /// <summary>Gets or sets the secure path to the uploaded company logo.</summary>
    public string LogoPath { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the company is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets the voluntary deduction floor policy.</summary>
    public NegativeNetPayPolicy NegativeNetPayPolicy { get; private set; } = NegativeNetPayPolicy.PartialDeduction;

    /// <summary>Gets a value indicating whether the first-run onboarding setup wizard is complete.</summary>
    public bool IsSetupComplete { get; private set; }

    /// <summary>Gets the timestamp when onboarding setup was marked complete.</summary>
    public DateTime? SetupCompletedUtc { get; private set; }

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

    /// <summary>Updates company profile details and contacts.</summary>
    public void ConfigureCompanyDetails(
        string tradingName,
        string tin,
        string fnpfNumber,
        string addr1,
        string addr2,
        string city,
        string phone,
        string email,
        string website,
        string country = "Fiji",
        string locale = "en-FJ",
        string logoPath = "")
    {
        if (string.IsNullOrWhiteSpace(tradingName))
            throw new ArgumentException("Trading name is required.", nameof(tradingName));
        if (string.IsNullOrWhiteSpace(tin) || tin.Length != 9)
            throw new ArgumentException("TIN must be a valid 9-digit registration number.", nameof(tin));

        TradingName = tradingName;
        TIN = tin;
        FnpfEmployerNumber = fnpfNumber;
        AddressLine1 = addr1;
        AddressLine2 = addr2;
        City = city;
        Phone = phone;
        Email = email;
        Website = website;
        Country = country;
        Locale = locale;
        LogoPath = logoPath;
    }

    /// <summary>Marks the first-run onboarding setup wizard completed.</summary>
    public void MarkSetupCompleted()
    {
        if (IsSetupComplete)
        {
            throw new InvalidOperationException("SETUP_ERROR: Onboarding setup is already complete for this tenant.");
        }

        IsSetupComplete = true;
        SetupCompletedUtc = DateTime.UtcNow;
    }

    /// <summary>Resets the first-run onboarding setup wizard completed state.</summary>
    public void ResetSetupState()
    {
        IsSetupComplete = false;
        SetupCompletedUtc = null;
    }

    /// <summary>Configures the voluntary deduction policy.</summary>
    public void SetNegativeNetPayPolicy(NegativeNetPayPolicy policy)
    {
        NegativeNetPayPolicy = policy;
    }
}

