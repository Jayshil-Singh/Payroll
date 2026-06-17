using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Workflows.Queries.GetPendingWorkflows;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// A wrapper view model around ApprovalWorkflow to support checkbox selection in the UI.
/// </summary>
public sealed partial class ApprovalWorkflowItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Gets the underlying workflow domain entity.</summary>
    public ApprovalWorkflow Workflow { get; }

    /// <summary>
    /// Initializes a new selection wrapper for a workflow entity.
    /// </summary>
    public ApprovalWorkflowItem(ApprovalWorkflow workflow)
    {
        Workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
    }
}

/// <summary>
/// ViewModel for the Approval Dashboard, providing statistics, status summaries, and navigation.
/// </summary>
public sealed partial class ApprovalDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private int _totalPendingCount;

    [ObservableProperty]
    private int _employeePendingCount;

    [ObservableProperty]
    private int _payrollRunPendingCount;

    [ObservableProperty]
    private int _otherPendingCount;

    /// <summary>Gets the panel title.</summary>
    public string Title => "Approval Dashboard";

    /// <summary>Gets the load statistics command.</summary>
    public IAsyncRelayCommand LoadStatsCommand { get; }

    /// <summary>Gets the navigation command to view the list of pending approvals.</summary>
    public IRelayCommand NavigateToPendingCommand { get; }

    /// <summary>
    /// Initializes dependencies and commands.
    /// </summary>
    public ApprovalDashboardViewModel(
        IMediator mediator,
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
        NavigateToPendingCommand = new RelayCommand(() => _navigationService.NavigateTo<PendingApprovalsViewModel>());

        // Initial load
        _ = LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        IsBusy = true;
        try
        {
            var workflows = await _mediator.Send(new GetPendingWorkflowsQuery());

            TotalPendingCount = workflows.Count;
            EmployeePendingCount = workflows.Count(w => w.EntityType.Equals("Employee", StringComparison.OrdinalIgnoreCase));
            PayrollRunPendingCount = workflows.Count(w => w.EntityType.Equals("PayrollRun", StringComparison.OrdinalIgnoreCase));
            OtherPendingCount = TotalPendingCount - (EmployeePendingCount + PayrollRunPendingCount);
        }
        catch (Exception ex)
        {
            _notificationService.Error($"Failed to load approval statistics: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// ViewModel for the detailed pending approvals workflow table and action pane.
/// </summary>
public sealed partial class PendingApprovalsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IApprovalEngine _approvalEngine;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty]
    private ApprovalWorkflowItem? _selectedWorkflowItem;

    [ObservableProperty]
    private string _comments = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedEntityTypeFilter = "All";

    /// <summary>Gets the page title.</summary>
    public string Title => "Pending Approvals";

    /// <summary>Gets the raw list of loaded workflow items.</summary>
    public ObservableCollection<ApprovalWorkflowItem> Workflows { get; } = new();

    /// <summary>Gets the filtered list of workflow items bound to the UI datagrid.</summary>
    public ObservableCollection<ApprovalWorkflowItem> FilteredWorkflows { get; } = new();

    /// <summary>Gets the list of audit log steps for the selected workflow request.</summary>
    public ObservableCollection<WorkflowStepLog> WorkflowSteps { get; } = new();

    /// <summary>Gets the filter options.</summary>
    public ObservableCollection<string> EntityTypeFilters { get; } = new() { "All", "Employee", "PayrollRun" };

    /// <summary>Gets the helper property for the raw domain entity from the selected wrapper item.</summary>
    public ApprovalWorkflow? SelectedWorkflow => SelectedWorkflowItem?.Workflow;

    /// <summary>Gets the load command.</summary>
    public IAsyncRelayCommand LoadWorkflowsCommand { get; }

    /// <summary>Gets the approval command.</summary>
    public IAsyncRelayCommand ApproveCommand { get; }

    /// <summary>Gets the rejection command.</summary>
    public IAsyncRelayCommand RejectCommand { get; }

    /// <summary>Gets the bulk approval command.</summary>
    public IAsyncRelayCommand BulkApproveCommand { get; }

    /// <summary>
    /// Initializes dependencies and commands.
    /// </summary>
    public PendingApprovalsViewModel(
        IMediator mediator,
        IApprovalEngine approvalEngine,
        INotificationService notificationService,
        IDialogService dialogService,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _approvalEngine = approvalEngine ?? throw new ArgumentNullException(nameof(approvalEngine));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

        LoadWorkflowsCommand = new AsyncRelayCommand(LoadWorkflowsAsync);
        ApproveCommand = new AsyncRelayCommand(ApproveAsync, () => SelectedWorkflow != null);
        RejectCommand = new AsyncRelayCommand(RejectAsync, () => SelectedWorkflow != null);
        BulkApproveCommand = new AsyncRelayCommand(BulkApproveAsync, () => Workflows.Any(w => w.IsSelected));

        // Initial load
        _ = LoadWorkflowsAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedEntityTypeFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedWorkflowItemChanged(ApprovalWorkflowItem? value)
    {
        WorkflowSteps.Clear();
        if (value != null)
        {
            foreach (var step in value.Workflow.Steps.OrderBy(s => s.TransitionedAt))
            {
                WorkflowSteps.Add(step);
            }
        }

        OnPropertyChanged(nameof(SelectedWorkflow));
        ApproveCommand.NotifyCanExecuteChanged();
        RejectCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadWorkflowsAsync()
    {
        IsBusy = true;
        Workflows.Clear();
        FilteredWorkflows.Clear();
        WorkflowSteps.Clear();
        SelectedWorkflowItem = null;
        Comments = string.Empty;

        try
        {
            var workflows = await _mediator.Send(new GetPendingWorkflowsQuery());
            foreach (var w in workflows)
            {
                var item = new ApprovalWorkflowItem(w);
                // Subscribe to checkbox changes to update BulkApprove executable state
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ApprovalWorkflowItem.IsSelected))
                    {
                        BulkApproveCommand.NotifyCanExecuteChanged();
                    }
                };
                Workflows.Add(item);
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            _notificationService.Error($"Failed to load pending workflows: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        FilteredWorkflows.Clear();
        var query = Workflows.AsEnumerable();

        if (SelectedEntityTypeFilter != "All")
        {
            query = query.Where(w => w.Workflow.EntityType.Equals(SelectedEntityTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(w => w.Workflow.EntityId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                     w.Workflow.RequestedBy.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            FilteredWorkflows.Add(item);
        }
    }

    private async Task ApproveAsync()
    {
        if (SelectedWorkflow == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Approve Request",
            $"Are you sure you want to approve this {SelectedWorkflow.EntityType} change request (ID: {SelectedWorkflow.EntityId})?");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _approvalEngine.ApproveAsync(
                SelectedWorkflow.WorkflowId,
                _currentUserService.Username,
                Comments);

            if (result.IsSuccess)
            {
                _notificationService.Success("Workflow request approved successfully.");
                await LoadWorkflowsAsync();
            }
            else
            {
                _notificationService.Error(string.Join(", ", result.Errors), "Approval Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error Approving Request");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RejectAsync()
    {
        if (SelectedWorkflow == null) return;

        if (string.IsNullOrWhiteSpace(Comments))
        {
            _notificationService.Warning("Comments/rejection reason is required to reject a request.");
            return;
        }

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Reject Request",
            $"Are you sure you want to reject this {SelectedWorkflow.EntityType} change request (ID: {SelectedWorkflow.EntityId})?");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _approvalEngine.RejectAsync(
                SelectedWorkflow.WorkflowId,
                _currentUserService.Username,
                Comments);

            if (result.IsSuccess)
            {
                _notificationService.Success("Workflow request rejected successfully.");
                await LoadWorkflowsAsync();
            }
            else
            {
                _notificationService.Error(string.Join(", ", result.Errors), "Rejection Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error Rejecting Request");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BulkApproveAsync()
    {
        var selected = Workflows.Where(w => w.IsSelected).ToList();
        if (!selected.Any())
        {
            _notificationService.Warning("No requests selected for bulk approval.");
            return;
        }

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Bulk Approval",
            $"Are you sure you want to approve all {selected.Count} selected requests?");

        if (!confirm) return;

        IsBusy = true;
        int successCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var item in selected)
            {
                var result = await _approvalEngine.ApproveAsync(
                    item.Workflow.WorkflowId,
                    _currentUserService.Username,
                    "Bulk approved.");

                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    errors.Add($"{item.Workflow.EntityType} ({item.Workflow.EntityId}): {string.Join(", ", result.Errors)}");
                }
            }

            if (errors.Any())
            {
                _notificationService.Warning($"Bulk approval completed with errors. Success: {successCount}, Failed: {errors.Count}");
            }
            else
            {
                _notificationService.Success($"Successfully approved {successCount} requests.");
            }

            await LoadWorkflowsAsync();
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error during Bulk Approval");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
