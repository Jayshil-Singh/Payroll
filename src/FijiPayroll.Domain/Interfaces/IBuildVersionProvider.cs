namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Provides system build version, git commit, and assembly details for non-repudiation binding.
/// </summary>
public interface IBuildVersionProvider
{
    /// <summary>Gets the main application version.</summary>
    string GetApplicationVersion();

    /// <summary>Gets the Git commit hash or build hash fallback.</summary>
    string GetGitCommitHash();

    /// <summary>Gets the DLL assembly version snapshot details.</summary>
    string GetAssemblyVersionSnapshot();

    /// <summary>Gets the combined SHA-256 system build version hash.</summary>
    string GetSystemBuildVersionHash();
}
