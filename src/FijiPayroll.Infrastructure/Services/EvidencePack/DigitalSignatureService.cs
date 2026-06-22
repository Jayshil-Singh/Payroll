using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Infrastructure.Services.ComplianceEvidence;
using System;
using System.Security.Cryptography;
using System.Text;

namespace FijiPayroll.Infrastructure.Services.EvidencePack;

/// <summary>
/// Infrastructure implementation of IDigitalSignatureService.
/// Integrates with KeyStorage to sign and verify compliance payloads.
/// </summary>
public sealed class DigitalSignatureService : IDigitalSignatureService
{
    /// <inheritdoc />
    public string SignData(string data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var rsa = KeyStorage.GetOrCreateKey();
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }

    /// <inheritdoc />
    public bool VerifySignature(string data, string signatureBase64)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(signatureBase64)) return false;

        try
        {
            using var rsa = KeyStorage.GetOrCreateKey();
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}
