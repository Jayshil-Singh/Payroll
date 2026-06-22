namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Interface for cryptographic signing and verification of compliance payloads.
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>
    /// Signs the given string data using the machine's RSA key.
    /// </summary>
    string SignData(string data);

    /// <summary>
    /// Verifies the given digital signature against the string data.
    /// </summary>
    bool VerifySignature(string data, string signatureBase64);
}
