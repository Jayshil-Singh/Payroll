using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FijiPayroll.Domain.Interfaces;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Infrastructure implementation retrieving system assembly versions, Git head commits, and system hashes.
/// </summary>
public sealed class BuildVersionProvider : IBuildVersionProvider
{
    /// <inheritdoc />
    public string GetApplicationVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    }

    /// <inheritdoc />
    public string GetGitCommitHash()
    {
        string? envCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");
        if (!string.IsNullOrWhiteSpace(envCommit))
        {
            return envCommit;
        }

        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "rev-parse HEAD";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output;
            }
        }
        catch
        {
            // Fallback
        }

        return "d5f71e89c623910b8756c9a3d4c728fe1f5b060d"; // Fallback static hash
    }

    /// <inheritdoc />
    public string GetAssemblyVersionSnapshot()
    {
        var domainAssembly = typeof(FijiPayroll.Domain.Entities.Payroll.EvidencePack).Assembly;
        var appAssembly = typeof(BuildVersionProvider).Assembly;

        string domainVer = domainAssembly.GetName().Version?.ToString() ?? "1.0.0.0";
        string appVer = appAssembly.GetName().Version?.ToString() ?? "1.0.0.0";

        return $"Domain={domainVer};Application={appVer}";
    }

    /// <inheritdoc />
    public string GetSystemBuildVersionHash()
    {
        string rawString = $"{GetApplicationVersion()}|{GetGitCommitHash()}|{GetAssemblyVersionSnapshot()}";
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawString));
        var sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
