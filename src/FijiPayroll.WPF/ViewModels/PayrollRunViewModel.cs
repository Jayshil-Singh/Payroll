using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing the Payroll Run operations and lifecycle.
/// Executes long-running computations asynchronously in the background.
/// </summary>
public sealed partial class PayrollRunViewModel : ObservableObject
{
    private readonly IPayrollRunService _payrollRunService;
    private int _companyId = 1; // Default company context

    [ObservableProperty]
    private PayrollRunDetailDto? _selectedRunDetail;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 25;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// List of payroll runs retrieved.
    /// </summary>
    public ObservableCollection<PayrollRunSummaryDto> Runs { get; } = new();

    public PayrollRunViewModel(IPayrollRunService payrollRunService)
    {
        _payrollRunService = payrollRunService;

        LoadRunsCommand = new AsyncRelayCommand(LoadRunsAsync);
        LoadRunDetailCommand = new AsyncRelayCommand<PayrollRunSummaryDto>(LoadRunDetailAsync);
        CalculateCommand = new AsyncRelayCommand<PayrollRunSummaryDto>(CalculateAsync);
        ResetCommand = new AsyncRelayCommand<PayrollRunSummaryDto>(ResetAsync);
        ApproveCommand = new AsyncRelayCommand<PayrollRunSummaryDto>(ApproveAsync);
        PostCommand = new AsyncRelayCommand<PayrollRunSummaryDto>(PostAsync);
        AdminOverrideLockCommand = new AsyncRelayCommand<PayrollRunSummaryDto>(AdminOverrideLockAsync);
    }

    public IAsyncRelayCommand LoadRunsCommand { get; }
    public IAsyncRelayCommand<PayrollRunSummaryDto> LoadRunDetailCommand { get; }
    public IAsyncRelayCommand<PayrollRunSummaryDto> CalculateCommand { get; }
    public IAsyncRelayCommand<PayrollRunSummaryDto> ResetCommand { get; }
    public IAsyncRelayCommand<PayrollRunSummaryDto> ApproveCommand { get; }
    public IAsyncRelayCommand<PayrollRunSummaryDto> PostCommand { get; }
    public IAsyncRelayCommand<PayrollRunSummaryDto> AdminOverrideLockCommand { get; }

    private async Task LoadRunsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _payrollRunService.GetListAsync(
                companyId: _companyId,
                pageNumber: PageNumber,
                pageSize: PageSize);

            if (result.IsSuccess && result.Value is not null)
            {
                Runs.Clear();
                foreach (var item in result.Value.Items)
                {
                    Runs.Add(item);
                }
                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages;
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load payroll runs.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRunDetailAsync(PayrollRunSummaryDto? run)
    {
        if (run is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _payrollRunService.GetByIdAsync(run.Id);
            if (result.IsSuccess)
            {
                SelectedRunDetail = result.Value;
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load payroll run details.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CalculateAsync(PayrollRunSummaryDto? run)
    {
        if (run is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // IDEMPOTENCY RULE: CalculateRequestId must be enforced from UI
            Guid requestId = Guid.NewGuid();

            // Run in background thread to prevent UI thread lockup on large datasets (10,000+ employees)
            var result = await Task.Run(() => _payrollRunService.CalculateAsync(run.Id, requestId));

            if (result.IsSuccess)
            {
                MessageBox.Show("Calculation completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadRunsAsync();
                if (SelectedRunDetail?.Id == run.Id)
                {
                    await LoadRunDetailAsync(run);
                }
            }
            else
            {
                MessageBox.Show(result.Error ?? "Calculation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResetAsync(PayrollRunSummaryDto? run)
    {
        if (run is null) return;

        var confirm = MessageBox.Show(
            "Are you sure you want to reset this run? Calculations will be marked as superseded.",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Reset must NOT automatically chain to recalculations or downstream processing
            var result = await Task.Run(() => _payrollRunService.ResetAsync(run.Id));
            if (result.IsSuccess)
            {
                MessageBox.Show("Run reset successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadRunsAsync();
                if (SelectedRunDetail?.Id == run.Id)
                {
                    await LoadRunDetailAsync(run);
                }
            }
            else
            {
                MessageBox.Show(result.Error ?? "Reset failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApproveAsync(PayrollRunSummaryDto? run)
    {
        if (run is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await Task.Run(() => _payrollRunService.ApproveAsync(run.Id));
            if (result.IsSuccess)
            {
                MessageBox.Show("Run approved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadRunsAsync();
                if (SelectedRunDetail?.Id == run.Id)
                {
                    await LoadRunDetailAsync(run);
                }
            }
            else
            {
                MessageBox.Show(result.Error ?? "Approval failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PostAsync(PayrollRunSummaryDto? run)
    {
        if (run is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await Task.Run(() => _payrollRunService.PostAsync(run.Id));
            if (result.IsSuccess)
            {
                MessageBox.Show("Run posted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadRunsAsync();
                if (SelectedRunDetail?.Id == run.Id)
                {
                    await LoadRunDetailAsync(run);
                }
            }
            else
            {
                MessageBox.Show(result.Error ?? "Posting failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AdminOverrideLockAsync(PayrollRunSummaryDto? run)
    {
        if (run is null) return;

        var confirm = MessageBox.Show(
            "WARNING: You are about to override the calculation lock. Proceed only if lock is stuck.",
            "Confirm Override",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await Task.Run(() => _payrollRunService.AdminOverrideLockAsync(run.Id));
            if (result.IsSuccess)
            {
                MessageBox.Show("Lock overridden. State is back to Draft.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadRunsAsync();
                if (SelectedRunDetail?.Id == run.Id)
                {
                    await LoadRunDetailAsync(run);
                }
            }
            else
            {
                MessageBox.Show(result.Error ?? "Override failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
