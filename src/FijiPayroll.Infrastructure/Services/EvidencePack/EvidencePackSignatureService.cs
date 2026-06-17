using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Interfaces;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Infrastructure service handling RSA-2048 signature generation and manifest placement.
/// </summary>
public sealed class EvidencePackSignatureService : IEvidencePackSignatureService
{
    private readonly IBuildVersionProvider _buildVersionProvider;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvidencePackSignatureService"/> class.
    /// </summary>
    public EvidencePackSignatureService(IBuildVersionProvider buildVersionProvider, IUnitOfWork unitOfWork)
    {
        _buildVersionProvider = buildVersionProvider ?? throw new ArgumentNullException(nameof(buildVersionProvider));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<byte[]> SignEvidenceZipAsync(
        byte[] unsignedZipBytes,
        int companyId,
        int payrollRunId,
        CancellationToken cancellationToken = default)
    {
        if (unsignedZipBytes == null) throw new ArgumentNullException(nameof(unsignedZipBytes));

        // Step 1 & 2: Compute SHA-256 hash of unsigned ZIP bytes
        using var sha256 = SHA256.Create();
        byte[] zipHashBytes = sha256.ComputeHash(unsignedZipBytes);
        string zipHashHex = BitConverter.ToString(zipHashBytes).Replace("-", "").ToLowerInvariant();

        // Step 3: Sign hash using RSA-2048 private key
        using var rsa = KeyStorage.GetOrCreateKey();
        byte[] signatureBytes = rsa.SignHash(zipHashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string signatureBase64 = Convert.ToBase64String(signatureBytes);

        // Step 4: Create signature.manifest.json
        var buildHash = _buildVersionProvider.GetSystemBuildVersionHash();
        var gitHash = _buildVersionProvider.GetGitCommitHash();
        var appVer = _buildVersionProvider.GetApplicationVersion();

        // Query the deterministic ledger timestamp from the database to guarantee byte-level determinism
        var ledgers = await _unitOfWork.Compliance.GetLedgerByRunIdAsync(payrollRunId, cancellationToken);
        var timestamp = ledgers.FirstOrDefault()?.CreatedUtc ?? DateTime.UtcNow;

        var manifestObj = new
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            ZipSha256 = zipHashHex,
            Signature = signatureBase64,
            BuildVersionHash = buildHash,
            GitCommitHash = gitHash,
            ApplicationVersion = appVer,
            Timestamp = timestamp
        };

        string manifestJson = System.Text.Json.JsonSerializer.Serialize(manifestObj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Step 5: Rebuild final ZIP by appending manifest
        using var ms = new MemoryStream();
        await ms.WriteAsync(unsignedZipBytes, 0, unsignedZipBytes.Length, cancellationToken);

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = archive.CreateEntry("signature.manifest.json", CompressionLevel.Optimal);
            entry.LastWriteTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var entryStream = entry.Open();
            byte[] jsonBytes = Encoding.UTF8.GetBytes(manifestJson);
            await entryStream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken);
        }

        return ms.ToArray();
    }
}

/// <summary>
/// Internal key storage management simulating secure machine keys.
/// </summary>
internal static class KeyStorage
{
    private static readonly string KeyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FijiPayroll",
        "evidence_pack_key.dat");

    private static readonly object _lock = new();
    private static RSA? _cachedKey;

    public static RSA GetOrCreateKey()
    {
        lock (_lock)
        {
            if (_cachedKey != null)
            {
                var rsaCopy = RSA.Create(2048);
                rsaCopy.ImportParameters(_cachedKey.ExportParameters(true));
                return rsaCopy;
            }

            var rsa = RSA.Create(2048);
            string? directory = Path.GetDirectoryName(KeyFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool loaded = false;
            if (File.Exists(KeyFilePath))
            {
                byte[] encryptedBytes = File.ReadAllBytes(KeyFilePath);
                try
                {
                    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
                    string pem = Encoding.UTF8.GetString(decryptedBytes);
                    rsa.ImportFromPem(pem);
                    loaded = true;
                }
                catch
                {
                    try
                    {
                        byte[] decryptedBytes = DecryptFallback(encryptedBytes);
                        string pem = Encoding.UTF8.GetString(decryptedBytes);
                        rsa.ImportFromPem(pem);
                        loaded = true;
                    }
                    catch
                    {
                        // Fall back to new key generation if decryption fails
                    }
                }
            }

            if (!loaded)
            {
                string newPem = rsa.ExportPkcs8PrivateKeyPem();
                byte[] rawBytes = Encoding.UTF8.GetBytes(newPem);
                byte[] encrypted;
                try
                {
                    encrypted = ProtectedData.Protect(rawBytes, null, DataProtectionScope.LocalMachine);
                }
                catch
                {
                    encrypted = EncryptFallback(rawBytes);
                }

                File.WriteAllBytes(KeyFilePath, encrypted);
            }

            _cachedKey = RSA.Create(2048);
            _cachedKey.ImportParameters(rsa.ExportParameters(true));
            return rsa;
        }
    }

    private static byte[] EncryptFallback(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = GetMachineKey();
        aes.IV = new byte[16];
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    private static byte[] DecryptFallback(byte[] cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = GetMachineKey();
        aes.IV = new byte[16];
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipherText, 0, cipherText.Length);
        }
        return ms.ToArray();
    }

    private static byte[] GetMachineKey()
    {
        string salt = Environment.MachineName + "FijiPayrollEvidenceSalt";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(salt));
    }
}
