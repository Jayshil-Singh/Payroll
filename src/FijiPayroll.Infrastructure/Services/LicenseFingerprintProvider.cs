using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the ILicenseFingerprintProvider.
/// Generates a resilient hardware fingerprint and installation ID.
/// </summary>
public sealed class LicenseFingerprintProvider : ILicenseFingerprintProvider
{
    private const string FolderName = "FijiPayroll";
    private const string FileName = "installation.dat";

    /// <inheritdoc />
    public Task<LicenseFingerprint> GenerateFingerprintAsync()
    {
        string installationId = GetOrCreateInstallationId();
        string machineIdHash = GenerateMachineIdHash();
        return Task.FromResult(new LicenseFingerprint(installationId, machineIdHash));
    }

    private string GetOrCreateInstallationId()
    {
        try
        {
            var commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), FolderName, FileName);
            if (File.Exists(commonPath))
            {
                var content = File.ReadAllText(commonPath).Trim();
                if (Guid.TryParse(content, out _))
                {
                    return content;
                }
            }

            // Check fallback path (LocalApplicationData)
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName, FileName);
            if (File.Exists(localPath))
            {
                var content = File.ReadAllText(localPath).Trim();
                if (Guid.TryParse(content, out _))
                {
                    // Restore to common path
                    WriteIdToFile(commonPath, content);
                    return content;
                }
            }

            // Generate new
            string newId = Guid.NewGuid().ToString();
            WriteIdToFile(commonPath, newId);
            WriteIdToFile(localPath, newId);
            return newId;
        }
        catch
        {
            // Fail-safe default installation ID
            return "00000000-0000-0000-0000-000000000000";
        }
    }

    private void WriteIdToFile(string path, string id)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, id);
        }
        catch
        {
            // Ignore write failures to ensure system resiliency
        }
    }

    private string GenerateMachineIdHash()
    {
        string cpuId = GetRegistryValue(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "Identifier", "CPU-Fallback");
        string systemUuid = GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemUUID", "UUID-Fallback");
        string baseBoardSerial = GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardSerialNumber", "Board-Fallback");
        string macAddress = GetPrimaryMacAddress() ?? "MAC-Fallback";

        string rawString = $"{cpuId}|{systemUuid}|{baseBoardSerial}|{macAddress}";
        return ComputeSha256(rawString);
    }

    private string GetRegistryValue(string keyPath, string valueName, string defaultValue)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key != null)
                {
                    var val = key.GetValue(valueName);
                    if (val != null)
                    {
                        return val.ToString() ?? defaultValue;
                    }
                }
            }
        }
        catch
        {
            // Suppress registry exceptions for resilience
        }
        return defaultValue;
    }

    private string? GetPrimaryMacAddress()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var primary = interfaces
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderBy(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 0 : 1) // Ethernet preferred
                .FirstOrDefault();

            if (primary != null)
            {
                return primary.GetPhysicalAddress().ToString();
            }
        }
        catch
        {
            // Suppress exceptions
        }
        return null;
    }

    private string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
