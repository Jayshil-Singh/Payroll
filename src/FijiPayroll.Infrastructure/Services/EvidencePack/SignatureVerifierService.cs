using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Infrastructure service verifying cryptographic RSA signatures and manifest boundaries.
/// </summary>
public sealed class SignatureVerifierService : ISignatureVerifierService
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureVerifierService"/> class.
    /// </summary>
    public SignatureVerifierService(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    /// <inheritdoc />
    public async Task VerifyEvidencePackSignatureAsync(
        byte[] signedZipBytes,
        CancellationToken cancellationToken = default)
    {
        if (signedZipBytes == null) throw new ArgumentNullException(nameof(signedZipBytes));

        ManifestContent? manifest = null;
        byte[] unsignedZipBytes;

        try
        {
            // 1. Extract signature.manifest.json
            using (var msRead = new MemoryStream(signedZipBytes))
            using (var archive = new ZipArchive(msRead, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry("signature.manifest.json");
                if (entry == null)
                {
                    throw new EvidencePackTamperedException("EVIDENCE_PACK_TAMPERED_EXCEPTION: signature.manifest.json is missing.");
                }

                using var entryStream = entry.Open();
                using var sr = new StreamReader(entryStream);
                string json = await sr.ReadToEndAsync(cancellationToken);
                manifest = System.Text.Json.JsonSerializer.Deserialize<ManifestContent>(json);
            }

            if (manifest == null)
            {
                throw new EvidencePackTamperedException("EVIDENCE_PACK_TAMPERED_EXCEPTION: signature.manifest.json is corrupt.");
            }

            // 2. Remove ONLY manifest entry from ZIP
            using var msUpdate = new MemoryStream();
            await msUpdate.WriteAsync(signedZipBytes, 0, signedZipBytes.Length, cancellationToken);

            using (var archive = new ZipArchive(msUpdate, ZipArchiveMode.Update, leaveOpen: true))
            {
                var entry = archive.GetEntry("signature.manifest.json");
                if (entry != null)
                {
                    entry.Delete();
                }
            }
            unsignedZipBytes = msUpdate.ToArray();
        }
        catch (EvidencePackTamperedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new EvidencePackTamperedException("EVIDENCE_PACK_TAMPERED_EXCEPTION: Failed to parse ZIP or extract manifest.", ex);
        }

        // 3. Recompute SHA-256 of remaining ZIP bytes
        using var sha256 = SHA256.Create();
        byte[] computedHashBytes = sha256.ComputeHash(unsignedZipBytes);
        string computedHashHex = BitConverter.ToString(computedHashBytes).Replace("-", "").ToLowerInvariant();

        // 4. Compare hash with manifest value
        if (!string.Equals(computedHashHex, manifest.ZipSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new EvidencePackTamperedException("EVIDENCE_PACK_TAMPERED_EXCEPTION: ZIP hash mismatch. The package has been modified.");
        }

        // 5. Validate RSA signature using public key
        try
        {
            using var rsa = KeyStorage.GetOrCreateKey();
            byte[] signatureBytes = Convert.FromBase64String(manifest.Signature);

            bool isSignatureValid = rsa.VerifyHash(computedHashBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!isSignatureValid)
            {
                throw new EvidencePackTamperedException("EVIDENCE_PACK_TAMPERED_EXCEPTION: Cryptographic signature is invalid.");
            }
        }
        catch (Exception ex) when (ex is not EvidencePackTamperedException)
        {
            throw new EvidencePackTamperedException("EVIDENCE_PACK_TAMPERED_EXCEPTION: Cryptographic signature verification failed.", ex);
        }

        // 6. Validate CompanyId matches current tenant context
        int currentCompanyId = _tenantProvider.GetCurrentCompanyId();
        if (manifest.CompanyId != currentCompanyId)
        {
            throw new EvidencePackTamperedException($"EVIDENCE_PACK_TAMPERED_EXCEPTION: Unauthorized context access. Evidence pack belongs to company {manifest.CompanyId}, but current tenant is {currentCompanyId}.");
        }
    }
}

/// <summary>
/// Serializable DTO matching the signature manifest schema.
/// </summary>
public sealed record ManifestContent(
    int CompanyId,
    int PayrollRunId,
    string ZipSha256,
    string Signature,
    string BuildVersionHash,
    string GitCommitHash,
    string ApplicationVersion,
    DateTime Timestamp
);
