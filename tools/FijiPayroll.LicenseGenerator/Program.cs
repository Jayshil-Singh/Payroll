using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Text.Json;

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

    private static void SafeSetColor(ConsoleColor color)
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                Console.ForegroundColor = color;
            }
        }
        catch { }
    }

    private static void SafeResetColor()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                Console.ResetColor();
            }
        }
        catch { }
    }

    private static void SafeWaitForKey()
    {
        try
        {
            if (!Console.IsInputRedirected)
            {
                Console.ReadKey(true);
            }
        }
        catch { }
    }

    private static int Main(string[] args)
    {
        bool interactive = args.Length == 0;
        try
        {
            SafeSetColor(ConsoleColor.Cyan);
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      Fiji Enterprise Payroll — License Generator v1.0       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            SafeResetColor();
            Console.WriteLine();

            string? company = null;
            string? expiryStr = null;
            string hardwareHash = "";
            string featureFlags = "*";
            string? privKeyPath = null;

            // Default output path: Desktop for interactive, local for CLI
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string defaultOutputPath = !string.IsNullOrEmpty(desktopPath) && interactive
                ? Path.Combine(desktopPath, "license.fplic")
                : "license.fplic";
            string outputPath = defaultOutputPath;

            // Try to load defaults from config.json if it exists
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("DefaultFeatures", out var featuresProp))
                    {
                        featureFlags = featuresProp.GetString() ?? "*";
                    }
                    if (root.TryGetProperty("PrivateKeyPath", out var keyPathProp))
                    {
                        string kPath = keyPathProp.GetString() ?? "";
                        if (!string.IsNullOrEmpty(kPath))
                        {
                            privKeyPath = Path.IsPathRooted(kPath) ? kPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, kPath);
                        }
                    }
                    if (root.TryGetProperty("DefaultExpiryDays", out var expiryDaysProp) && expiryDaysProp.TryGetInt32(out int days))
                    {
                        expiryStr = DateTime.Today.AddDays(days).ToString("yyyy-MM-dd");
                    }
                }
                catch (Exception ex)
                {
                    SafeSetColor(ConsoleColor.Yellow);
                    Console.WriteLine($"Warning: Failed to load config.json: {ex.Message}");
                    SafeResetColor();
                }
            }

            if (interactive)
            {
                SafeSetColor(ConsoleColor.Yellow);
                Console.WriteLine("--- Interactive License Generation Wizard ---");
                SafeResetColor();
                Console.WriteLine();

                while (string.IsNullOrWhiteSpace(company))
                {
                    Console.Write("Enter Company/Tenant Name (Required): ");
                    company = Console.ReadLine()?.Trim();
                }

                string defaultExpiry = expiryStr ?? "";
                expiryStr = null; // Clear to force prompt but use default as fallback

                string expiryPrompt = string.IsNullOrEmpty(defaultExpiry)
                    ? "Enter Expiry Date (yyyy-MM-dd, Required): "
                    : $"Enter Expiry Date (yyyy-MM-dd, Required, press Enter for default '{defaultExpiry}'): ";

                while (string.IsNullOrWhiteSpace(expiryStr))
                {
                    Console.Write(expiryPrompt);
                    string? inputExpiry = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(inputExpiry) && !string.IsNullOrEmpty(defaultExpiry))
                    {
                        expiryStr = defaultExpiry;
                        break;
                    }
                    else if (!string.IsNullOrEmpty(inputExpiry))
                    {
                        if (DateTime.TryParseExact(inputExpiry, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                        {
                            expiryStr = inputExpiry;
                        }
                        else
                        {
                            SafeSetColor(ConsoleColor.Red);
                            Console.WriteLine("Invalid date format. Please use yyyy-MM-dd.");
                            SafeResetColor();
                        }
                    }
                }

                Console.Write("Enter Hardware Fingerprint (Optional, press Enter to bypass): ");
                hardwareHash = Console.ReadLine()?.Trim() ?? string.Empty;

                Console.Write($"Enter Feature Flags (Optional, default '{featureFlags}'): ");
                string? inputFeatures = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(inputFeatures))
                {
                    featureFlags = inputFeatures;
                }

                Console.Write($"Enter Output File Path (Optional, default '{defaultOutputPath}'): ");
                string? inputOutput = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(inputOutput))
                {
                    outputPath = inputOutput;
                }

                string keyPrompt = string.IsNullOrEmpty(privKeyPath)
                    ? "Enter Private Key File Path (Optional, press Enter to use default dev key): "
                    : $"Enter Private Key File Path (Optional, press Enter to use configured '{privKeyPath}'): ";

                Console.Write(keyPrompt);
                string? inputKey = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(inputKey))
                {
                    privKeyPath = inputKey;
                }
                Console.WriteLine();
            }
            else
            {
                if (args.Contains("--help") || args.Contains("-h"))
                {
                    PrintUsage();
                    return 0;
                }

                // Parse arguments
                company = GetArgValue(args, "--company");
                expiryStr = GetArgValue(args, "--expiry");
                hardwareHash = GetArgValue(args, "--hash") ?? string.Empty;
                featureFlags = GetArgValue(args, "--features") ?? featureFlags;
                privKeyPath = GetArgValue(args, "--privkey") ?? privKeyPath;
                outputPath = GetArgValue(args, "--output") ?? outputPath;
            }

            // Validate required arguments for non-interactive
            if (!interactive)
            {
                if (string.IsNullOrWhiteSpace(company))
                {
                    SafeSetColor(ConsoleColor.Red);
                    Console.WriteLine("ERROR: --company is required.");
                    SafeResetColor();
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(expiryStr) || !DateTime.TryParseExact(expiryStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    SafeSetColor(ConsoleColor.Red);
                    Console.WriteLine("ERROR: --expiry is required and must be in yyyy-MM-dd format.");
                    SafeResetColor();
                    return 1;
                }
            }

            // Load private key
            string privateKeyPem;
            if (!string.IsNullOrEmpty(privKeyPath))
            {
                if (!File.Exists(privKeyPath))
                {
                    if (!interactive)
                    {
                        SafeSetColor(ConsoleColor.Red);
                        Console.WriteLine($"ERROR: Private key file not found: {privKeyPath}");
                        SafeResetColor();
                        return 1;
                    }
                    else
                    {
                        SafeSetColor(ConsoleColor.Yellow);
                        Console.WriteLine($"Warning: Private key not found at {privKeyPath}. Using default embedded development key.");
                        SafeResetColor();
                        privateKeyPem = DefaultPrivateKeyPem;
                    }
                }
                else
                {
                    privateKeyPem = File.ReadAllText(privKeyPath);
                }
            }
            else
            {
                SafeSetColor(ConsoleColor.Yellow);
                Console.WriteLine("INFO: Using default embedded development private key.");
                SafeResetColor();
                privateKeyPem = DefaultPrivateKeyPem;
            }

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

            // Ensure output folder directory exists
            string? outDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            File.WriteAllText(outputPath, licenseXml.ToString());

            SafeSetColor(ConsoleColor.Green);
            Console.WriteLine();
            Console.WriteLine($"✔ License generated successfully.");
            SafeResetColor();
            Console.WriteLine();
            Console.WriteLine($"  Company:       {company}");
            Console.WriteLine($"  Expiry Date:   {expiryStr}");
            Console.WriteLine($"  Hardware Hash: {(string.IsNullOrEmpty(hardwareHash) ? "(any machine)" : hardwareHash)}");
            Console.WriteLine($"  Features:      {featureFlags}");
            Console.WriteLine($"  Output File:   {Path.GetFullPath(outputPath)}");
            Console.WriteLine();

            if (interactive)
            {
                SafeSetColor(ConsoleColor.Yellow);
                Console.WriteLine("Press any key to exit...");
                SafeResetColor();
                SafeWaitForKey();
            }

            return 0;
        }
        catch (Exception ex)
        {
            SafeSetColor(ConsoleColor.Red);
            Console.WriteLine();
            Console.WriteLine("FATAL ERROR: Failed to run or generate license.");
            Console.WriteLine($"Details: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            SafeResetColor();

            if (interactive)
            {
                SafeSetColor(ConsoleColor.Yellow);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                SafeResetColor();
                SafeWaitForKey();
            }
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
