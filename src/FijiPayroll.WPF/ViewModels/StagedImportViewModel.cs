using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel backing the Staged Import pipeline dashboard/dialog.
/// </summary>
public sealed partial class StagedImportViewModel : ObservableObject
{
    private readonly IImportEngine _importEngine;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _selectedModule = "Employees"; // Employees, Lookups

    [ObservableProperty]
    private bool _isValidating;

    [ObservableProperty]
    private bool _isCommitting;

    [ObservableProperty]
    private string _statusText = "Ready to Import";

    [ObservableProperty]
    private int _recordsProcessed;

    [ObservableProperty]
    private int _successCount;

    [ObservableProperty]
    private int _failureCount;

    [ObservableProperty]
    private bool _isValid;

    private Guid _jobId;

    /// <summary>
    /// Gets the list of available modules for import.
    /// </summary>
    public ObservableCollection<string> Modules { get; } = new() { "Employees", "Lookups" };

    /// <summary>
    /// Gets the list of validation errors found in the staged spreadsheet data.
    /// </summary>
    public ObservableCollection<ImportError> Errors { get; } = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="StagedImportViewModel"/> class.
    /// </summary>
    public StagedImportViewModel(IImportEngine importEngine)
    {
        _importEngine = importEngine;
        BrowseFileCommand = new RelayCommand(BrowseFile);
        ValidateCommand = new AsyncRelayCommand(ValidateImportAsync);
        CommitCommand = new AsyncRelayCommand(CommitImportAsync, () => IsValid && !IsCommitting);
    }

    /// <summary>Gets the browse file command.</summary>
    public IRelayCommand BrowseFileCommand { get; }

    /// <summary>Gets the validate command.</summary>
    public IAsyncRelayCommand ValidateCommand { get; }

    /// <summary>Gets the commit command.</summary>
    public IAsyncRelayCommand CommitCommand { get; }

    private void BrowseFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Select Import Spreadsheet File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            FilePath = openFileDialog.FileName;
        }
    }

    private async Task ValidateImportAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            MessageBox.Show("Please select a valid import file first.", "Import Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsValidating = true;
        StatusText = "Analyzing and validating spreadsheet structure...";
        Errors.Clear();
        IsValid = false;
        CommitCommand.NotifyCanExecuteChanged();

        try
        {
            using var fileStream = File.OpenRead(FilePath);
            var result = await _importEngine.ValidateImportAsync(fileStream, SelectedModule);

            _jobId = result.JobId;
            RecordsProcessed = result.RecordsProcessed;
            SuccessCount = result.SuccessCount;
            FailureCount = result.FailureCount;
            IsValid = result.IsValid;

            foreach (var err in result.Errors)
            {
                Errors.Add(err);
            }

            if (IsValid)
            {
                StatusText = "Validation succeeded! All rows are correct. You can now commit this import.";
            }
            else
            {
                StatusText = $"Validation completed with errors. Staged rows contain {FailureCount} failures.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Import analysis failed: {ex.Message}";
            MessageBox.Show($"Failed to analyze file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsValidating = false;
            CommitCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task CommitImportAsync()
    {
        if (_jobId == Guid.Empty)
        {
            MessageBox.Show("No active import session found to commit.", "Import Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsCommitting = true;
        StatusText = "Applying import transactions to the database...";

        try
        {
            await _importEngine.CommitImportAsync(_jobId);
            StatusText = "Import committed successfully! All master files updated.";
            MessageBox.Show("Spreadsheet data successfully committed to database.", "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);
            IsValid = false;
        }
        catch (Exception ex)
        {
            StatusText = $"Commit failed: {ex.Message}";
            MessageBox.Show($"Failed to apply changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCommitting = false;
            CommitCommand.NotifyCanExecuteChanged();
        }
    }
}
