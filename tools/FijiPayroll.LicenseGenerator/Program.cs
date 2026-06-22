using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

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
    private const string DefaultPrivateKeyPem = @"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEAu3juMOvG8UfEny/uAT2plJR6aDZg5xPPyPK8VZzc7FjNA5IW
MzRs/MDPd9qx4+DoNVuz5ofcpJAlzmRPXn5YSfTmVYPV+Bh2weFkHivxbxK+sutj
rV5ckFEzihZVbfO64XaZowceVN8vjoRtJK1eCSZITlcztZ1Zc+W0vNWHqx+IC+hx
l8J0qxiEzQHercrNxGe/88h982oxatkotucc abHTXGCkJ0MClDtri0ow9yffCNBh
74ZRjkQ1MT8cBm817URPlh94ruG6c4eQ2MjBx4xmYfWkJ3Ev7DydUxtz/xT4tSsJ
Fs+NKxkyg8dyqZMExzJyURx971xTL/mh7TEQNQIDAQABAoIBABaKUCj44U0GQHQP
kx9Nq3sPAbO0LgL7jwMzTq8mWlR3Z2z0FZ0zk6YIe6LZ/k1wYh5Pqpjx0yt1fBi
TDeWiYb0OJbKxK1cjn3sNF10eDfCmXpKfQ+xdygTYx0e0yEWg+kXYkBs4FzS8CEy
JrLMHOIMFB9Y1p2IVJ7QQ4XHFPX9kNNFXq3M1bT10XfDZLMy5zBLlK7RsYQJdBtP
ZvpaOm8LNe+tRhcFnz0Q9L9eVbtgK6BgJlCPqfXfcR6VLpM3QdIVJWBb4A4P9i5G
h3RTFqSMevH/fIslB3lXSMOvgXY3p2t1IVF0S8/uvLxm5q/rXBiUbR0lm5yC1fVl
jD7PBuECgYEA4o8UJn3aF5HYKrZfCfz3MAx3bfZGh5g1RcU2j+1T5q5InVp7Nrkd
RW1K/i5lBMl1UBJlXq3b1MS+Sp8HEyRRhZ2V5O7PRm5FAKjR4bY5+K0fy9mX1IAf
NlPMh8u3pXwRQFzMP7K39Q9sIKb3LcGN3hYYF5uMJX9RJlhQ8g6dB6UCgYEA1BVH
p6PFQO+1ig4kxrtFX0u1enXJl0M6EI4J/1DfXxRq6yBrCq+Nh5Gg31HhP9rOWqwk
TxdL0I6YR1+v+J/q3GxB/n/9cR/Nw5oJh2OwR7N3lX7jtV0Yr2K6k3r1YDpFB14Z
OX3/LXcQX2C4xbGJ1tLCz5evqfXIT50LUdD/gXkCgYEAsDa8wWly/M7d7bGyhmqy
8jLsE17Kb/0pGBwySQFqs0GB8dKBY5i2gl07clkz7GFXpBCfR+lGHNr6Pz3GjFaS
A7JitY1g0IR6gYx0DRFALYXU9B/2q0rPJzRF6bJ3EqYZ7x0udIyWC2PQE5vFJoJl
IG96lXT+uBE9USB5lNhgqnECgYEAgYIk4/yfS6/jLJkQ4MwvFCB+vOT7BN1SHC0K
1bj/kBqyPkiPBA9tL0qHOQ8pwEUMLf1kJLKHCdhCqP7Imf1kAHDI6Crlfl3LQw/K
Sp5qZTlKP9rWd1yZlXt53g3sPL3fOHQ/EOLl3u1C5/9IN0TI8v7pX/vR8sIIzD/j
6j/oJaECgYBdGz/L1nEr35i4OfzrPqGI8ZnLdgb1BIb3m2yk1DpGzsDfzkaLfwJo
na9mD7QI0FzX1eN3C5IbNJIx/me2MIKbB/YVkDB18bKW4Uy1amjVGJfvJCCn2dzF
3h0H8j4F3fVMij/cLnADhOqaE9HGPVE3cGJikShk8c/Ks4nVzEL5vQ==
-----END RSA PRIVATE KEY-----";

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
