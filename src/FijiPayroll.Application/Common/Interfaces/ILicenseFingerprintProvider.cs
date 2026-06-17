using FijiPayroll.Application.Common.Models;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Service interface for generating unique system hardware fingerprints.
/// </summary>
public interface ILicenseFingerprintProvider
{
    /// <summary>
    /// Generates a resilient hardware fingerprint containing the Installation ID and Machine ID Hash.
    /// </summary>
    /// <returns>A task representing the asynchronous fingerprint generation, containing the LicenseFingerprint.</returns>
    Task<LicenseFingerprint> GenerateFingerprintAsync();
}
