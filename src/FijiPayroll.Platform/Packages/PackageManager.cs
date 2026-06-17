using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Platform.Packages;

/// <summary>
/// Manages validation, checksum verification, and cryptographic signature checking on enterprise package distributions.
/// </summary>
public sealed class PackageManager
{
    private readonly ILogger<PackageManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageManager"/> class.
    /// </summary>
    public PackageManager(ILogger<PackageManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Computes the SHA256 checksum of a package file.
    /// </summary>
    /// <param name="filePath">Path to the package file.</param>
    /// <returns>Uppercase hexadecimal SHA256 hash string.</returns>
    public string ComputeChecksum(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("Target file not found.", filePath);

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hashBytes = sha256.ComputeHash(stream);

        var sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Verifies the cryptographic RSA signature of a package's hash.
    /// </summary>
    /// <param name="packagePath">The path to the package file.</param>
    /// <param name="signatureBase64">The base64-encoded signature of the package hash.</param>
    /// <param name="publicKeyPem">The RSA public key in PEM format.</param>
    /// <returns>True if the signature is valid; otherwise false.</returns>
    public bool VerifyPackageSignature(string packagePath, string signatureBase64, string publicKeyPem)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(packagePath) || string.IsNullOrWhiteSpace(signatureBase64) || string.IsNullOrWhiteSpace(publicKeyPem))
            {
                return false;
            }

            string computedHashHex = ComputeChecksum(packagePath);
            byte[] hashBytes = Encoding.UTF8.GetBytes(computedHashHex);
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            return rsa.VerifyData(hashBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature for package: {PackagePath}", packagePath);
            return false;
        }
    }
}
