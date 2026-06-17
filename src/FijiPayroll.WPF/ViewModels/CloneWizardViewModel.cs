using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Features.PayrollComponents.Commands.CloneComponents;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// View model backing the Company Components Clone Wizard screen.
/// Coordinates multi-company rules copying and conflict resolutions.
/// </summary>
public sealed partial class CloneWizardViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private int _sourceCompanyId = 1;

    [ObservableProperty]
    private int _targetCompanyId = 2;

    [ObservableProperty]
    private string _selectedMode = "Merge"; // Merge, Replace, Skip

    [ObservableProperty]
    private bool _isCloning;

    [ObservableProperty]
    private int _clonedCount;

    [ObservableProperty]
    private int _skippedCount;

    [ObservableProperty]
    private int _replacedCount;

    /// <summary>
    /// Gets the list of log messages from the clone transaction.
    /// </summary>
    public ObservableCollection<string> LogMessages { get; } = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="CloneWizardViewModel"/> class.
    /// </summary>
    public CloneWizardViewModel(IMediator mediator)
    {
        _mediator = mediator;
        ExecuteCloneCommand = new AsyncRelayCommand(ExecuteCloneAsync);
    }

    /// <summary>Gets the execute clone command.</summary>
    public IAsyncRelayCommand ExecuteCloneCommand { get; }

    private async Task ExecuteCloneAsync()
    {
        if (SourceCompanyId == TargetCompanyId)
        {
            MessageBox.Show("Source and target companies cannot be the same.", "Clone Wizard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsCloning = true;
        LogMessages.Clear();
        LogMessages.Add("[Info] Starting clone transaction...");

        try
        {
            var command = new CloneComponentsCommand(SourceCompanyId, TargetCompanyId, SelectedMode, Array.Empty<int>());
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value is not null)
            {
                ClonedCount = result.Value.ClonedCount;
                SkippedCount = result.Value.SkippedCount;
                ReplacedCount = result.Value.ReplacedCount;

                foreach (var log in result.Value.LogMessages)
                {
                    LogMessages.Add(log);
                }

                LogMessages.Add($"[Success] Clone process completed successfully! Cloned: {ClonedCount}, Replaced: {ReplacedCount}, Skipped: {SkippedCount}.");
            }
            else
            {
                var errorMsg = result.Error ?? "Clone execution failed.";
                LogMessages.Add($"[Error] {errorMsg}");
                MessageBox.Show(errorMsg, "Clone Wizard", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"[Exception] {ex.Message}");
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCloning = false;
        }
    }
}
