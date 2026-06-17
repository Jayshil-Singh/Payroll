using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Enumerations;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel for the read-only Payroll Component listing screen.
/// Follows MVVM using CommunityToolkit.Mvvm and consumes <see cref="IPayrollComponentService"/> directly.
/// </summary>
public sealed partial class PayrollComponentViewModel : ObservableObject
{
    private readonly IPayrollComponentService _componentService;
    private readonly IServiceProvider _serviceProvider;
    private int _companyId = 1; // Default company context

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private ComponentType? _selectedTypeFilter;

    [ObservableProperty]
    private bool _activeOnly = true;

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
    /// Gets the list of payroll component summaries.
    /// </summary>
    public ObservableCollection<PayrollComponentSummaryDto> Components { get; } = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollComponentViewModel"/> class.
    /// </summary>
    /// <param name="componentService">The payroll component application service.</param>
    /// <param name="serviceProvider">The DI service provider for resolving editor windows.</param>
    public PayrollComponentViewModel(IPayrollComponentService componentService, IServiceProvider serviceProvider)
    {
        _componentService = componentService;
        _serviceProvider  = serviceProvider;

        LoadComponentsCommand = new AsyncRelayCommand(LoadComponentsAsync);
        NextPageCommand = new AsyncRelayCommand(GoToNextPageAsync, () => PageNumber < TotalPages);
        PreviousPageCommand = new AsyncRelayCommand(GoToPreviousPageAsync, () => PageNumber > 1);
        ToggleActiveCommand = new AsyncRelayCommand<PayrollComponentSummaryDto>(ToggleActiveAsync);
        DuplicateCommand = new AsyncRelayCommand<PayrollComponentSummaryDto>(DuplicateAsync);
        DeleteCommand = new AsyncRelayCommand<PayrollComponentSummaryDto>(DeleteAsync);
        CreateNewCommand = new AsyncRelayCommand(CreateNewAsync);
        EditCommand = new AsyncRelayCommand<PayrollComponentSummaryDto>(EditAsync);
        OpenSimulationCommand = new RelayCommand(OpenSimulation);
        OpenCloneWizardCommand = new RelayCommand(OpenCloneWizard);
        OpenPackageManagerCommand = new RelayCommand(OpenPackageManager);
    }

    /// <summary>Gets the load components command.</summary>
    public IAsyncRelayCommand LoadComponentsCommand { get; }

    /// <summary>Gets the next page command.</summary>
    public IAsyncRelayCommand NextPageCommand { get; }

    /// <summary>Gets the previous page command.</summary>
    public IAsyncRelayCommand PreviousPageCommand { get; }

    /// <summary>Gets the toggle active status command.</summary>
    public IAsyncRelayCommand<PayrollComponentSummaryDto> ToggleActiveCommand { get; }

    /// <summary>Gets the duplicate component command.</summary>
    public IAsyncRelayCommand<PayrollComponentSummaryDto> DuplicateCommand { get; }

    /// <summary>Gets the delete component command.</summary>
    public IAsyncRelayCommand<PayrollComponentSummaryDto> DeleteCommand { get; }

    /// <summary>Gets the command that opens the editor window in create mode.</summary>
    public IAsyncRelayCommand CreateNewCommand { get; }

    /// <summary>Gets the command that opens the editor window in edit mode for the selected component.</summary>
    public IAsyncRelayCommand<PayrollComponentSummaryDto> EditCommand { get; }

    /// <summary>Gets the command that opens the simulation panel.</summary>
    public IRelayCommand OpenSimulationCommand { get; }

    /// <summary>Gets the command that opens the clone wizard.</summary>
    public IRelayCommand OpenCloneWizardCommand { get; }

    /// <summary>Gets the command that opens the package manager dashboard.</summary>
    public IRelayCommand OpenPackageManagerCommand { get; }

    /// <summary>
    /// Loads components based on search terms, filter types, and page numbers.
    /// </summary>
    private async Task LoadComponentsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _componentService.GetListAsync(
                companyId: _companyId,
                searchTerm: SearchTerm,
                typeFilter: SelectedTypeFilter,
                activeOnly: ActiveOnly,
                pageNumber: PageNumber,
                pageSize: PageSize);

            if (result.IsSuccess && result.Value is not null)
            {
                Components.Clear();
                foreach (var item in result.Value.Items)
                {
                    Components.Add(item);
                }

                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages;

                NextPageCommand.NotifyCanExecuteChanged();
                PreviousPageCommand.NotifyCanExecuteChanged();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load components.";
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

    private async Task GoToNextPageAsync()
    {
        if (PageNumber < TotalPages)
        {
            PageNumber++;
            await LoadComponentsAsync();
        }
    }

    private async Task GoToPreviousPageAsync()
    {
        if (PageNumber > 1)
        {
            PageNumber--;
            await LoadComponentsAsync();
        }
    }

    private async Task ToggleActiveAsync(PayrollComponentSummaryDto? dto)
    {
        if (dto is null) return;

        IsLoading = true;
        try
        {
            var result = await _componentService.ToggleActiveAsync(dto.Id, !dto.IsActive);
            if (result.IsSuccess)
            {
                await LoadComponentsAsync(); // Refresh list to apply any display logic
            }
            else
            {
                MessageBox.Show(result.Error ?? "Failed to update component status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private async Task DuplicateAsync(PayrollComponentSummaryDto? dto)
    {
        if (dto is null) return;

        // In this phase (read-only listing + commands, forms next phase), we present a basic dialog request.
        // For production WPF UI, a pop-up input window is typically used.
        string newCode = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter code for duplicated component:", "Duplicate Component", $"{dto.ComponentCode}_COPY");

        if (string.IsNullOrWhiteSpace(newCode)) return;

        string newName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter name for duplicated component:", "Duplicate Component", $"Copy of {dto.ComponentName}");

        if (string.IsNullOrWhiteSpace(newName)) return;

        IsLoading = true;
        try
        {
            var result = await _componentService.DuplicateAsync(dto.Id, newCode.Trim(), newName.Trim());
            if (result.IsSuccess)
            {
                await LoadComponentsAsync(); // Refresh list
            }
            else
            {
                MessageBox.Show(result.Error ?? "Failed to duplicate component.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private async Task DeleteAsync(PayrollComponentSummaryDto? dto)
    {
        if (dto is null) return;

        var confirm = MessageBox.Show(
            $"Are you sure you want to delete component '{dto.ComponentCode}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var result = await _componentService.DeleteAsync(dto.Id);
            if (result.IsSuccess)
            {
                await LoadComponentsAsync();
            }
            else
            {
                MessageBox.Show(result.Error ?? "Failed to delete component.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private async Task CreateNewAsync(CancellationToken cancellationToken)
    {
        var formVm = _serviceProvider.GetRequiredService<PayrollComponentFormViewModel>();
        var window = new Views.PayrollComponentEditorWindow(formVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        bool saved = window.ShowDialog() == true;
        if (saved)
        {
            await LoadComponentsAsync();
        }
    }

    private async Task EditAsync(PayrollComponentSummaryDto? dto, CancellationToken cancellationToken)
    {
        if (dto is null) return;

        var formVm = _serviceProvider.GetRequiredService<PayrollComponentFormViewModel>();

        // Load existing data into the form ViewModel before showing the window
        await formVm.LoadForEditAsync(dto.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(formVm.ErrorMessage))
        {
            MessageBox.Show(formVm.ErrorMessage, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var window = new Views.PayrollComponentEditorWindow(formVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        bool saved = window.ShowDialog() == true;
        if (saved)
        {
            await LoadComponentsAsync();
        }
    }

    private void OpenSimulation()
    {
        var vm = _serviceProvider.GetRequiredService<ComponentSimulationViewModel>();
        var window = new Views.ComponentSimulationWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    private void OpenCloneWizard()
    {
        var vm = _serviceProvider.GetRequiredService<CloneWizardViewModel>();
        var window = new Views.CloneWizardWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    private void OpenPackageManager()
    {
        var vm = _serviceProvider.GetRequiredService<PackageManagerViewModel>();
        var window = new Views.PackageManagerWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }
}
