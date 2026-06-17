using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Cryptographically signs completed compliance evidence packs.
/// </summary>
public interface IEvidencePackSignatureService
{
    /// <summary>
    /// Computes the ZIP hash, signs it via RSA, creates signature.manifest.json, and appends it to rebuild the final signed ZIP bytes.
    /// </summary>
    /// <param name="unsignedZipBytes">The compiled ZIP bytes containing all reports and manifests except the signature manifest.</param>
    /// <param name="companyId">The company ID context.</param>
    /// <param name="payrollRunId">The payroll run ID context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deterministic, signed ZIP bytes.</returns>
    Task<byte[]> SignEvidenceZipAsync(
        byte[] unsignedZipBytes,
        int companyId,
        int payrollRunId,
        CancellationToken cancellationToken = default);
}
