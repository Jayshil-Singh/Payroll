using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.SDK.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FijiPayroll.Infrastructure.Services.Licensing;

/// <summary>
/// Infrastructure implementation of license checking and validation using RSA signatures.
/// Supports offline validation, fingerprint validation, and custom public key overrides.
/// </summary>
public sealed class LicenseValidator : ILicenseProvider
{
    private readonly ILicenseFingerprintProvider _fingerprintProvider;

    /// <summary>
    /// Default embedded development public key PEM.
    /// </summary>
    public const string DefaultPublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA5nJoprX7bOjfV3h5EC4C
Z4Fgk/eOgseKTGgs9GmLHeGVBrI2FxK7jwN/2mtxhb859vyadYi3ArPZ+8eIBg9G
xSRCOBSpRL4aQjauc2wb566KGrAU/3ssU/SNFlwSNC4J+0zoNlTYr/AtdZB4Enny
fNuoD/6QURGuEsd0w8mgzitT8VmH/fwkPIUM3wyWtLAkd+eAhr/sbRG8WXZ6gMlX
X8M+EKw3uabiuf9cR/vD8qECvLQPaULYSbOOQrZhZj9mNV9YlaElUcO5npWKMqc9
81bQjA3L2oCfe0C96kPIQt6u/HjpdPEJ+C88O82Q1zHuZONbzcVboitutVG1MCIZ
7QIDAQAB
-----END PUBLIC KEY-----";

    /// <summary>Gets a value indicating whether the application has a valid active license.</summary>
    public bool IsLicensed { get; private set; }

    /// <summary>Gets the number of days remaining until the license expires.</summary>
    public int DaysRemaining { get; private set; }

    /// <summary>Gets the licensed company name.</summary>
    public string Company { get; private set; } = string.Empty;

    /// <summary>Gets the license expiry date.</summary>
    public DateTime ExpiryDate { get; private set; } = DateTime.MinValue;

    /// <summary>Gets the raw comma-separated feature flags string.</summary>
    public string FeatureFlags { get; private set; } = string.Empty;

    /// <summary>Gets the hardware machine hash string matched in the license.</summary>
    public string HardwareHash { get; private set; } = string.Empty;

    /// <summary>Gets the error description if validation fails.</summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseValidator"/> class.
    /// </summary>
    public LicenseValidator(ILicenseFingerprintProvider fingerprintProvider)
    {
        _fingerprintProvider = fingerprintProvider ?? throw new ArgumentNullException(nameof(fingerprintProvider));
    }

    /// <summary>
    /// Loads and validates the default offline license file (license.fplic) from application base directory.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.fplic");
            if (!File.Exists(licensePath))
            {
                IsLicensed = false;
                ErrorMessage = "License file (license.fplic) not found.";
                return;
            }

            string licenseXml = await File.ReadAllTextAsync(licensePath);
            await ValidateLicenseFileAsync(licenseXml);
        }
        catch (Exception ex)
        {
            IsLicensed = false;
            ErrorMessage = $"Failed to read license file: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates raw license XML using local public key overrides or default embedded PEM.
    /// </summary>
    public async Task<bool> ValidateLicenseFileAsync(string licenseXml)
    {
        try
        {
            string publicKeyPem = DefaultPublicKeyPem;
            string overrideKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public_key.pem");
            if (File.Exists(overrideKeyPath))
            {
                publicKeyPem = await File.ReadAllTextAsync(overrideKeyPath);
            }

            return await ValidateLicenseInternalAsync(licenseXml, publicKeyPem);
        }
        catch (Exception ex)
        {
            IsLicensed = false;
            ErrorMessage = $"Validation failed: {ex.Message}";
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> ValidateLicenseAsync(
        string licenseXml,
        string publicKeyPem,
        CancellationToken cancellationToken = default)
    {
        return ValidateLicenseInternalAsync(licenseXml, publicKeyPem);
    }

    private async Task<bool> ValidateLicenseInternalAsync(string licenseXml, string publicKeyPem)
    {
        try
        {
            var doc = XDocument.Parse(licenseXml);
            var root = doc.Element("License");
            if (root == null)
            {
                IsLicensed = false;
                ErrorMessage = "Invalid license XML structure.";
                return false;
            }

            var company = root.Element("Company")?.Value ?? string.Empty;
            var expiryStr = root.Element("ExpiryDate")?.Value ?? string.Empty;
            var hardwareHash = root.Element("HardwareHash")?.Value ?? string.Empty;
            var featureFlags = root.Element("FeatureFlags")?.Value ?? string.Empty;
            var signature = root.Element("Signature")?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(company) || string.IsNullOrEmpty(expiryStr) || string.IsNullOrEmpty(signature))
            {
                IsLicensed = false;
                ErrorMessage = "Required license elements are missing.";
                return false;
            }

            // Verify signature
            string canonicalMessage = $"Company={company}&ExpiryDate={expiryStr}&HardwareHash={hardwareHash}&FeatureFlags={featureFlags}";
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            byte[] messageBytes = Encoding.UTF8.GetBytes(canonicalMessage);
            byte[] signatureBytes = Convert.FromBase64String(signature);

            bool isSignatureValid = rsa.VerifyData(messageBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!isSignatureValid)
            {
                IsLicensed = false;
                ErrorMessage = "License signature verification failed (TAMP_WARN).";
                return false;
            }

            // Verify hardware fingerprint if specified
            if (!string.IsNullOrEmpty(hardwareHash))
            {
                var currentFingerprint = await _fingerprintProvider.GenerateFingerprintAsync();
                if (!string.Equals(hardwareHash, currentFingerprint.MachineIdHash, StringComparison.OrdinalIgnoreCase))
                {
                    IsLicensed = false;
                    ErrorMessage = "License hardware fingerprint mismatch.";
                    return false;
                }
            }

            // Verify expiry
            if (!DateTime.TryParseExact(expiryStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var expiryDate))
            {
                IsLicensed = false;
                ErrorMessage = "Invalid ExpiryDate format (expected yyyy-MM-dd).";
                return false;
            }

            if (DateTime.UtcNow.Date > expiryDate.Date)
            {
                IsLicensed = false;
                DaysRemaining = 0;
                ErrorMessage = $"License expired on {expiryDate:yyyy-MM-dd}.";
                return false;
            }

            IsLicensed = true;
            Company = company;
            ExpiryDate = expiryDate;
            HardwareHash = hardwareHash;
            FeatureFlags = featureFlags;
            DaysRemaining = (expiryDate.Date - DateTime.UtcNow.Date).Days;
            ErrorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            IsLicensed = false;
            ErrorMessage = $"License parsing failed: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Checks if the licensed features contain the requested flag.
    /// </summary>
    public bool HasFeature(string featureName)
    {
        if (!IsLicensed) return false;
        if (string.IsNullOrEmpty(FeatureFlags)) return false;
        if (FeatureFlags.Equals("*") || FeatureFlags.Contains("All", StringComparison.OrdinalIgnoreCase)) return true;

        var parts = FeatureFlags.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p.Trim().Equals(featureName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
