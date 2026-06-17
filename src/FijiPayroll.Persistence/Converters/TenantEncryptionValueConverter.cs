using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FijiPayroll.Persistence.Converters;

/// <summary>
/// Value converter that encrypts and decrypts string values per tenant, with key versioning metadata prepended.
/// Prefix format: [Algorithm]:[KeyVersion]:[KeyIdentifier]:[CipherText]
/// </summary>
public sealed class TenantEncryptionValueConverter : ValueConverter<string, string>
{
    private static readonly AsyncLocal<string?> _currentKey = new();

    /// <summary>
    /// Gets or sets the thread/async-scoped tenant security key used for the active operation.
    /// </summary>
    public static string? CurrentKey
    {
        get => _currentKey.Value;
        set => _currentKey.Value = value;
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="TenantEncryptionValueConverter"/> class.
    /// </summary>
    public TenantEncryptionValueConverter()
        : base(
            v => Encrypt(v),
            v => Decrypt(v))
    {
    }

    /// <summary>
    /// Encrypts plain text using the current tenant's security key.
    /// Format: AES256:v1:[KeyIdentifier]:[Base64CipherText]
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        string? key = CurrentKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            // Fallback for migrations, seeding, or unit tests without an active tenant context
            return $"PLAINTEXT:v1:none:{Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText))}";
        }

        // Standardise key size to 32 bytes (256 bits) for AES-256
        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        byte[] iv = new byte[16];
        RandomNumberGenerator.Fill(iv);

        byte[] cipherBytes;
        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                }
                cipherBytes = ms.ToArray();
            }
        }

        // Combine IV and CipherText
        byte[] combined = new byte[iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, iv.Length, cipherBytes.Length);

        string cipherTextBase64 = Convert.ToBase64String(combined);
        string keyIdentifier = GetKeyIdentifier(key);

        return $"AES256:v1:{keyIdentifier}:{cipherTextBase64}";
    }

    /// <summary>
    /// Decrypts a formatted cipher text string.
    /// Supports both active AES256:v1 and PLAINTEXT fallback formats.
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        string[] parts = cipherText.Split(':');
        if (parts.Length < 4)
        {
            // Non-conforming string, return as is
            return cipherText;
        }

        string algorithm = parts[0];
        string version = parts[1];
        string keyIdentifier = parts[2];
        string payload = parts[3];

        if (algorithm == "PLAINTEXT")
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }

        if (algorithm != "AES256" || version != "v1")
        {
            throw new NotSupportedException($"ENCRYPTION_ERROR: Unsupported encryption algorithm '{algorithm}' or version '{version}'.");
        }

        string? key = CurrentKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            // If key context is missing, return a masked placeholder or try to decrypt with key identifier if supported.
            return "****** (Encrypted)";
        }

        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        byte[] combined = Convert.FromBase64String(payload);

        if (combined.Length < 16)
            throw new FormatException("ENCRYPTION_ERROR: Invalid payload length.");

        byte[] iv = new byte[16];
        byte[] cipherBytes = new byte[combined.Length - 16];
        Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(combined, iv.Length, cipherBytes, 0, cipherBytes.Length);

        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cs, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }

    private static string GetKeyIdentifier(string key)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}
