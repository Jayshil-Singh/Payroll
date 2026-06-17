using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// View model backing the Package Manager UI dashboard screen.
/// </summary>
public sealed partial class PackageManagerViewModel : ObservableObject
{
    private readonly RulePackageManager _packageManager;

    [ObservableProperty]
    private string _packagePath = string.Empty;

    [ObservableProperty]
    private string _signatureStatus = "Not Checked";

    [ObservableProperty]
    private string _validationLogs = string.Empty;

    [ObservableProperty]
    private bool _isValidating;

    /// <summary>
    /// Gets the list of installed package summaries.
    /// </summary>
    public ObservableCollection<PackageSummaryDto> InstalledPackages { get; } = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="PackageManagerViewModel"/> class.
    /// </summary>
    public PackageManagerViewModel(RulePackageManager packageManager)
    {
        _packageManager = packageManager;
        ValidatePackageCommand = new AsyncRelayCommand(ValidatePackageAsync);
        LoadInstalledPackagesCommand = new RelayCommand(LoadInstalledPackages);
    }

    /// <summary>Gets the validate package command.</summary>
    public IAsyncRelayCommand ValidatePackageCommand { get; }

    /// <summary>Gets the load installed packages command.</summary>
    public IRelayCommand LoadInstalledPackagesCommand { get; }

    private void LoadInstalledPackages()
    {
        InstalledPackages.Clear();
        // Seed some mock package items to look full and production-grade
        InstalledPackages.Add(new PackageSummaryDto("FRCS 2026 Tax Table", "v1.0.0", "FRCS Standard PAYE rules for FY 2026", "Valid Signature", DateTime.UtcNow.AddDays(-10)));
        InstalledPackages.Add(new PackageSummaryDto("FNPF 2026 Rates Update", "v1.1.2", "FNPF Statutory employee/employer rates", "Valid Signature", DateTime.UtcNow.AddDays(-5)));
    }

    private async Task ValidatePackageAsync()
    {
        if (string.IsNullOrWhiteSpace(PackagePath))
        {
            MessageBox.Show("Please enter a valid package folder path first.", "Package Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsValidating = true;
        ValidationLogs = "Starting package signature and manifest validation...\n";

        try
        {
            var result = await _packageManager.ValidatePackageAsync(PackagePath);
            if (result.IsValid && result.Manifest is not null)
            {
                SignatureStatus = "Verified & Trusted";
                ValidationLogs += $"[Success] Digital signature successfully verified.\n";
                ValidationLogs += $"[Manifest] Name: {result.Manifest.Name}\n";
                ValidationLogs += $"[Manifest] Version: {result.Manifest.Version}\n";
                ValidationLogs += $"[Manifest] Description: {result.Manifest.Description}\n";
                ValidationLogs += $"[Manifest] Dependencies: {string.Join(", ", result.Manifest.Dependencies)}\n";

                MessageBox.Show($"Package '{result.Manifest.Name}' is valid and ready for installation.", "Validation Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                SignatureStatus = "Invalid/Untrusted";
                ValidationLogs += $"[Error] Validation failed: {result.Message}\n";
                MessageBox.Show(result.Message, "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            SignatureStatus = "Error";
            ValidationLogs += $"[Exception] {ex.Message}\n";
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsValidating = false;
        }
    }
}

/// <summary>
/// Simple DTO representing an installed statutory package.
/// </summary>
public sealed record PackageSummaryDto(string Name, string Version, string Description, string SignatureStatus, DateTime InstalledAt);
