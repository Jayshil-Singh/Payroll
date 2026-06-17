using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Payroll;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Defines the domain contract for generating the compliance evidence pack and packaging it into a ZIP archive.
/// </summary>
public interface IEvidencePackGeneratorService
{
    /// <summary>
    /// Generates a complete, structured compliance evidence pack for a finalized payroll run.
    /// </summary>
    /// <param name="companyId">The company tenant context identifier.</param>
    /// <param name="payrollRunId">The payroll run identifier.</param>
    /// <param name="requestedBy">The username of the user triggering the generation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A structured <see cref="EvidencePack"/> domain model.</returns>
    Task<EvidencePack> GenerateEvidencePackAsync(
        int companyId,
        int payrollRunId,
        string requestedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Packages the evidence pack and all its associated artifacts (JSON, PDF, SSRS snapshots) into a ZIP archive.
    /// </summary>
    /// <param name="evidencePack">The generated evidence pack domain model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The byte array representing the ZIP archive.</returns>
    Task<byte[]> GenerateEvidenceZipArchiveAsync(
        EvidencePack evidencePack,
        CancellationToken cancellationToken = default);
}
