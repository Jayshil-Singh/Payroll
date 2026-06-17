using FijiPayroll.Application.Services;
using FluentAssertions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests.Formula;

/// <summary>
/// Integration unit tests for RulePackageManager package validation.
/// </summary>
public sealed class RulePackageManagerTests : IDisposable
{
    private readonly string _tempFolder;

    public RulePackageManagerTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "FijiPayroll_PackageTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }
        catch
        {
            // Ignore clean up errors
        }
    }

    [Fact]
    public async Task ValidatePackageAsync_MissingFolder_ReturnsError()
    {
        // Arrange
        var manager = new RulePackageManager();
        var nonExistentPath = Path.Combine(_tempFolder, "NonExistentFolder");

        // Act
        var result = await manager.ValidatePackageAsync(nonExistentPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("does not exist");
    }

    [Fact]
    public async Task ValidatePackageAsync_MissingManifest_ReturnsError()
    {
        // Arrange
        var manager = new RulePackageManager();
        // folder exists but is empty

        // Act
        var result = await manager.ValidatePackageAsync(_tempFolder);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("missing 'manifest.json'");
    }

    [Fact]
    public async Task ValidatePackageAsync_MissingSignature_ReturnsError()
    {
        // Arrange
        var manager = new RulePackageManager();
        var manifestPath = Path.Combine(_tempFolder, "manifest.json");
        var manifest = new RulePackageManager.PackageManifest
        {
            Name = "FRCS 2026 Tax Table",
            Version = "v1.0.0"
        };
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Act
        var result = await manager.ValidatePackageAsync(_tempFolder);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("missing 'signature.sig'");
    }

    [Fact]
    public async Task ValidatePackageAsync_ValidManifestAndSignature_ReturnsSuccess()
    {
        // Arrange
        var manager = new RulePackageManager();
        var manifestPath = Path.Combine(_tempFolder, "manifest.json");
        var signaturePath = Path.Combine(_tempFolder, "signature.sig");

        var manifest = new RulePackageManager.PackageManifest
        {
            Name = "FRCS 2026 Tax Table",
            Version = "1.0.0",
            Description = "Frcs tax values for 2026",
            Dependencies = new System.Collections.Generic.List<string> { "BaseRules" }
        };
        var manifestJson = JsonSerializer.Serialize(manifest);
        await File.WriteAllTextAsync(manifestPath, manifestJson);
        
        // Write valid hex string signature
        var sigBytes = new byte[] { 1, 2, 3, 4, 5 };
        var sigHex = Convert.ToHexString(sigBytes);
        await File.WriteAllTextAsync(signaturePath, sigHex);

        // Act
        var result = await manager.ValidatePackageAsync(_tempFolder);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Manifest.Should().NotBeNull();
        result.Manifest!.Name.Should().Be("FRCS 2026 Tax Table");
        result.Manifest!.Version.Should().Be("1.0.0");
    }
}
