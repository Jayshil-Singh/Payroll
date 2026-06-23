using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace FijiPayroll.LicenseGenerator;

/// <summary>
/// Fiji Enterprise Payroll — Offline License Generator.
/// Generates RSA-signed .fplic license files for offline distribution.
///
/// Usage:
///   FijiPayroll.LicenseGenerator.exe --company "Fiji National Corp" --expiry 2027-06-30 --hash ABCD1234 --features "Reports,Compliance,Payroll"
///
/// Options:
///   --company   Company tenant name (required)
///   --expiry    Expiry date in yyyy-MM-dd format (required)
///   --hash      Hardware fingerprint hash (optional, leave empty for any-machine license)
///   --features  Comma-separated feature flags, or "*" for all features (optional, defaults to "*")
///   --privkey   Path to private key PEM file (optional, uses default dev key if omitted)
///   --output    Output .fplic file path (optional, defaults to "./license.fplic")
/// </summary>
internal static class Program
{
    /// <summary>
    /// Default embedded development RSA private key PEM.
    /// Must correspond to the public key embedded in LicenseValidator.
    /// WARNING: In production releases, replace this key and pass via --privkey argument.
    /// </summary>
    private const string DefaultPrivateKeyPem = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDmcmimtfts6N9X
eHkQLgJngWCT946Cx4pMaCz0aYsd4ZUGsjYXEruPA3/aa3GFvzn2/Jp1iLcCs9n7
x4gGD0bFJEI4FKlEvhpCNq5zbBvnrooasBT/eyxT9I0WXBI0Lgn7TOg2VNiv8C11
kHgSefJ826gP/pBREa4Sx3TDyaDOK1PxWYf9/CQ8hQzfDJa0sCR354CGv+xtEbxZ
dnqAyVdfwz4QrDe5puK5/1xH+8PyoQK8tA9pQthJs45CtmFmP2Y1X1iVoSVRw7me
lYoypz3zVtCMDcvagJ97QL3qQ8hC3q78eOl08Qn4Lzw7zZDXMe5k41vNxVuiK261
UbUwIhntAgMBAAECggEAePwh0zyBlqkf8IVQUd1F5991u9lhWWm3QuwChgMPRY3U
NqLDYRO1opy8uAhmnkhJ/1CZKxGuu11/GP+le0Dz77ZciaLXRz7i/FZG+lQMxnLN
ELvXGlYpbJ5coBuQdxKgrO2wkC21YZEf3LQPRev+Ee0ka9lDHTzB/hv3Qn8NzI7V
xiq/Blu/6aRkpLPzitQn+MeCqN9ipaD1RD12D38LC4vKErSTJSSIHFgNsuTulr44
K+VWnBq0JlggtGkTB58WIFpPVy51FnMcAxhgk7LwOjtL2QUDVU5oNbsB2kPXSFgJ
91t5mrTVA2bcM4TJDbM8E9rK75TMMWu16qWWAb9FRQKBgQDxtuGB7er9WBwgN3cP
xPeYc/9xgr1RruRjmeFGrO4GOaPTm81V6tc12sRWKHDp0paV6riaDCJFgFzI/hmO
+WeZJaHKZmA4KkoskivyPrPo9ZA2rhbYPWXEs/B25TarBcHp9OJf1M+W1hynXimO
v0LQV9a90UzokQemIBJMgtEC4wKBgQD0EQ1QFBvZYDM3x3JY6Xz1bFIaRwo4MgTR
4/92lxF8coV8QAoTCvBnSnHZ/MqRTSzf4Hy1WPNsaKgyhoc79/+4aUgiVy4V3lPJ
q8CFhcrXmQ8yJ1mOT52Zl7k4G3TWBLKBGpCo9oCdJaFIadTuf9vRm7JSmQwRUGX4
aCmzVqJ47wKBgCLHo88kQsnNYc6o7HLSbqX7GuhkXYVhWu+R9r5Kp70xkgcixfr0
3Z6cKeAT1Ztvd8d+jK1tzYienbs2BMtzy5pXtd3/uRybySx8o+Ipb423t9aGWjcn
LnuNQK568NDO9UYKvH/5iR01Fc5nWCd4Ec8UtIt/kEduhuE6gCeOMzDtAoGBANO5
+zeNyj8AAk6QOfVB0EJDztG28PmhAqdmR9aD5Qp1erE4CMVORxED9tJpRv1X2ub4
IpdbbAiOneL+61Atquw6gPYxdOxJq5wW3/O1BiuUPyd+FWWsUYbNpUM0Jl4HQydW
eUnqVdZ9r3VXQf4IcxRaIg8fb+WRPnSJQCuwq0+HAoGAQxXdJd57QFJlBfGC8fVw
Bpt9NRSuf4x5HSiszgKwBBvl6D2QAYU45clmkef/D8W8cf1QU0HVaYZDsPSu/8DF
m75FHgmaBulP4bsXlmjjtImzx9S85nmIYDrID2tPo4+DYd4uIYx6sd+adyRHcD6p
1K15G/WAXOnhGwovc5F8DMk=
-----END PRIVATE KEY-----";

    private static int Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Fiji Enterprise Payroll — License Generator v1.0       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        // Parse arguments
        string? company = GetArgValue(args, "--company");
        string? expiryStr = GetArgValue(args, "--expiry");
        string hardwareHash = GetArgValue(args, "--hash") ?? string.Empty;
        string featureFlags = GetArgValue(args, "--features") ?? "*";
        string? privKeyPath = GetArgValue(args, "--privkey");
        string outputPath = GetArgValue(args, "--output") ?? "license.fplic";

        // Validate required arguments
        if (string.IsNullOrWhiteSpace(company))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: --company is required.");
            Console.ResetColor();
            return 1;
        }

        if (string.IsNullOrWhiteSpace(expiryStr) || !DateTime.TryParseExact(expiryStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var expiryDate))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: --expiry is required and must be in yyyy-MM-dd format.");
            Console.ResetColor();
            return 1;
        }

        // Load private key
        string privateKeyPem;
        if (!string.IsNullOrEmpty(privKeyPath))
        {
            if (!File.Exists(privKeyPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Private key file not found: {privKeyPath}");
                Console.ResetColor();
                return 1;
            }
            privateKeyPem = File.ReadAllText(privKeyPath);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("INFO: Using default embedded development private key.");
            Console.ResetColor();
            privateKeyPem = DefaultPrivateKeyPem;
        }

        try
        {
            // Build the canonical message exactly as LicenseValidator expects
            string canonicalMessage = $"Company={company}&ExpiryDate={expiryStr}&HardwareHash={hardwareHash}&FeatureFlags={featureFlags}";

            // Sign with RSA-SHA256
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            byte[] messageBytes = Encoding.UTF8.GetBytes(canonicalMessage);
            byte[] signatureBytes = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string signature = Convert.ToBase64String(signatureBytes);

            // Build license XML
            var licenseXml = new XDocument(
                new XElement("License",
                    new XElement("Company", company),
                    new XElement("ExpiryDate", expiryStr),
                    new XElement("HardwareHash", hardwareHash),
                    new XElement("FeatureFlags", featureFlags),
                    new XElement("Signature", signature),
                    new XElement("GeneratedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement("GeneratorVersion", "1.0.0")
                )
            );

            File.WriteAllText(outputPath, licenseXml.ToString());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"✔ License generated successfully.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"  Company:       {company}");
            Console.WriteLine($"  Expiry Date:   {expiryStr}");
            Console.WriteLine($"  Hardware Hash: {(string.IsNullOrEmpty(hardwareHash) ? "(any machine)" : hardwareHash)}");
            Console.WriteLine($"  Features:      {featureFlags}");
            Console.WriteLine($"  Output File:   {Path.GetFullPath(outputPath)}");
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FATAL: Failed to generate license: {ex.Message}");
            Console.ResetColor();
            return 2;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  FijiPayroll.LicenseGenerator --company \"Company Name\" --expiry 2027-06-30 [options]");
        Console.WriteLine();
        Console.WriteLine("Required:");
        Console.WriteLine("  --company    Licensed company/tenant name");
        Console.WriteLine("  --expiry     License expiry date (yyyy-MM-dd format)");
        Console.WriteLine();
        Console.WriteLine("Optional:");
        Console.WriteLine("  --hash       Hardware fingerprint hash (omit for any-machine license)");
        Console.WriteLine("  --features   Comma-separated feature flags or '*' for all (default: *)");
        Console.WriteLine("  --privkey    Path to RSA private key PEM file (uses default dev key if omitted)");
        Console.WriteLine("  --output     Output .fplic file path (default: ./license.fplic)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Generate an unrestricted developer license");
        Console.WriteLine("  FijiPayroll.LicenseGenerator --company \"Dev Corp\" --expiry 2027-12-31 --features \"*\"");
        Console.WriteLine();
        Console.WriteLine("  # Generate a production license with hardware lock");
        Console.WriteLine("  FijiPayroll.LicenseGenerator --company \"Fiji National\" --expiry 2026-12-31 \\");
        Console.WriteLine("    --hash A3B1C2D4 --features \"Payroll,Reports\" --privkey ./prod_private.pem");
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
