using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Verifies the cryptographic signature and manifest integrity of compliance evidence packs.
/// </summary>
public interface ISignatureVerifierService
{
    /// <summary>
    /// Extracts the signature manifest, reconstructs the unsigned zip bytes, computes the SHA-256 hash,
    /// verifies the RSA signature, and enforces tenant isolation checks.
    /// </summary>
    /// <param name="signedZipBytes">The fully compiled signed ZIP bytes containing the signature manifest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="FijiPayroll.Application.Common.Exceptions.EvidencePackTamperedException">Thrown if signature, hash verification, or tenant validation fails.</exception>
    Task VerifyEvidencePackSignatureAsync(
        byte[] signedZipBytes,
        CancellationToken cancellationToken = default);
}
