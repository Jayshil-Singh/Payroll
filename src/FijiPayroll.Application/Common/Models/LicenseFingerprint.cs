namespace FijiPayroll.Application.Common.Models;

/// <summary>
/// Represents a hardened machine license fingerprint.
/// </summary>
public sealed record LicenseFingerprint
{
    /// <summary>
    /// Initialises a new instance of the <see cref="LicenseFingerprint"/> record.
    /// </summary>
    /// <param name="installationId">The unique installation identifier.</param>
    /// <param name="machineIdHash">The hardware-tied machine identification hash.</param>
    public LicenseFingerprint(string installationId, string machineIdHash)
    {
        InstallationId = installationId;
        MachineIdHash = machineIdHash;
    }

    /// <summary>
    /// Gets the unique installation identifier.
    /// </summary>
    public string InstallationId { get; init; }

    /// <summary>
    /// Gets the hardware-tied machine identification hash.
    /// </summary>
    public string MachineIdHash { get; init; }
}
