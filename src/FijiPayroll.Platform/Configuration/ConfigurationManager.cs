using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FijiPayroll.Platform.Configuration;

/// <summary>
/// Manages company configuration export and import operations using the .companyconfig format.
/// Supports optional AES encryption.
/// </summary>
public sealed class ConfigurationManager
{
    private static readonly byte[] Salt = [0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76];

    /// <summary>
    /// Exports the configuration object to a .companyconfig file.
    /// </summary>
    /// <typeparam name="T">The configuration model type.</typeparam>
    /// <param name="filePath">The target file path to write to.</param>
    /// <param name="configData">The configuration data object.</param>
    /// <param name="encryptionKey">Optional key to encrypt the configuration content.</param>
    public async Task ExportConfigAsync<T>(string filePath, T configData, string? encryptionKey = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (configData == null)
        {
            throw new ArgumentNullException(nameof(configData));
        }

        string json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });

        if (!string.IsNullOrWhiteSpace(encryptionKey))
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] cipherBytes = EncryptBytes(plainBytes, encryptionKey);
            await File.WriteAllBytesAsync(filePath, cipherBytes);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Imports configuration from a .companyconfig file.
    /// </summary>
    /// <typeparam name="T">The configuration model type.</typeparam>
    /// <param name="filePath">The source file path to read from.</param>
    /// <param name="encryptionKey">Optional key used to decrypt the configuration content.</param>
    /// <returns>The deserialized configuration object.</returns>
    public async Task<T?> ImportConfigAsync<T>(string filePath, string? encryptionKey = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Configuration file not found.", filePath);
        }

        if (!string.IsNullOrWhiteSpace(encryptionKey))
        {
            byte[] cipherBytes = await File.ReadAllBytesAsync(filePath);
            byte[] plainBytes = DecryptBytes(cipherBytes, encryptionKey);
            string json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        else
        {
            string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<T>(json);
        }
    }

    private static byte[] EncryptBytes(byte[] plainBytes, string key)
    {
        using var aes = Aes.Create();
        using var rfc = new Rfc2898DeriveBytes(key, Salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = rfc.GetBytes(32);
        aes.IV = rfc.GetBytes(16);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plainBytes, 0, plainBytes.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    private static byte[] DecryptBytes(byte[] cipherBytes, string key)
    {
        using var aes = Aes.Create();
        using var rfc = new Rfc2898DeriveBytes(key, Salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = rfc.GetBytes(32);
        aes.IV = rfc.GetBytes(16);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }
}
