using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Defines the contract for validating company licenses using RSA signatures.
/// </summary>
public interface ILicenseProvider
{
    /// <summary>
    /// Validates the license signature against a public key.
    /// </summary>
    /// <param name="licenseXml">The serialized license XML string containing the signature.</param>
    /// <param name="publicKeyPem">The RSA public key in PEM format used for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the license signature is valid; otherwise false.</returns>
    Task<bool> ValidateLicenseAsync(
        string licenseXml,
        string publicKeyPem,
        CancellationToken cancellationToken = default);
}
