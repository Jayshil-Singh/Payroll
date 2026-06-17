using System;
using System.Security.Cryptography;
using System.Text;

namespace FijiPayroll.Application.Services.EvidencePack;

/// <summary>
/// Cryptographic utility for calculating deterministic SHA-256 hashes across compliance data.
/// </summary>
public static class DeterministicHashGenerator
{
    /// <summary>
    /// Computes the SHA-256 hash of a string input using UTF-8 encoding.
    /// </summary>
    public static string ComputeSha256Hash(string input)
    {
        if (input == null) return string.Empty;
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes the SHA-256 hash of a raw byte array.
    /// </summary>
    public static string ComputeSha256Hash(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return string.Empty;
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
