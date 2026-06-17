using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.CompanySetup.Commands.CreateCompanyWizard;
using FijiPayroll.Application.Features.CompanySetup.Queries.ValidateCompanyWizard;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// Model representing an item in the checklist display.
/// </summary>
public sealed class SetupTaskItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private bool _isCompleted;
    private string? _completedBy;
    private DateTime? _completedUtc;

    /// <summary>Gets the wizard step.</summary>
    public WizardStep Step { get; }

    /// <summary>Gets the step title.</summary>
    public string Title { get; }

    /// <summary>Gets the step description.</summary>
    public string Description { get; }

    /// <summary>Gets or sets a value indicating whether this step is completed.</summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>Gets or sets who completed the step.</summary>
    public string? CompletedBy
    {
        get => _completedBy;
        set => SetProperty(ref _completedBy, value);
    }

    /// <summary>Gets or sets the completion date and time.</summary>
    public DateTime? CompletedUtc
    {
        get => _completedUtc;
        set => SetProperty(ref _completedUtc, value);
    }

    /// <summary>Initializes a new instance of the SetupTaskItem.</summary>
    public SetupTaskItem(WizardStep step, string title, string description)
    {
        Step = step;
        Title = title;
        Description = description;
    }
}

/// <summary>
/// View model managing the guided Company Onboarding Setup Wizard.
/// </summary>
public sealed class CompanySetupDashboardViewModel : ViewModelBase
{
    private readonly ISetupWorkflowService _workflowService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;

    private WizardStep _currentStep = WizardStep.Welcome;
    private bool _isSetupComplete;
    private DateTime? _setupCompletedUtc;
    private bool _isValid;
    private bool _hasRunValidation;
    private string? _errorMessage;
    private string? _successMessage;

    /// <summary>Gets the checklist tasks.</summary>
    public ObservableCollection<SetupTaskItem> Tasks { get; } = new();

    /// <summary>Gets the validation errors.</summary>
    public ObservableCollection<string> ValidationErrors { get; } = new();

    /// <summary>Gets the validation warnings.</summary>
    public ObservableCollection<string> ValidationWarnings { get; } = new();

    /// <summary>Gets the panel title.</summary>
    public string TitleText => "Company Onboarding Setup Wizard";

    /// <summary>Gets or sets the current step.</summary>
    public WizardStep CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(CanCompleteStep));
                OnPropertyChanged(nameof(CanSkipStep));
                OnPropertyChanged(nameof(IsValidationStep));
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether setup is complete.</summary>
    public bool IsSetupComplete
    {
        get => _isSetupComplete;
        set => SetProperty(ref _isSetupComplete, value);
    }

    /// <summary>Gets or sets setup completion date.</summary>
    public DateTime? SetupCompletedUtc
    {
        get => _setupCompletedUtc;
        set => SetProperty(ref _setupCompletedUtc, value);
    }

    /// <summary>Gets or sets a value indicating whether the setup is valid.</summary>
    public bool IsValid
    {
        get => _isValid;
        set => SetProperty(ref _isValid, value);
    }

    /// <summary>Gets or sets a value indicating whether validation was run.</summary>
    public bool HasRunValidation
    {
        get => _hasRunValidation;
        set => SetProperty(ref _hasRunValidation, value);
    }

    /// <summary>Gets or sets the error message.</summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>Gets or sets the success message.</summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    /// <summary>Gets if the current step can be completed.</summary>
    public bool CanCompleteStep => CurrentStep < WizardStep.Validation;

    /// <summary>Gets if the current step can be skipped.</summary>
    public bool CanSkipStep => CurrentStep > WizardStep.Welcome && CurrentStep < WizardStep.Validation;

    /// <summary>Gets if the current step is the validation step.</summary>
    public bool IsValidationStep => CurrentStep == WizardStep.Validation;

    /// <summary>Gets the command to load state.</summary>
    public IAsyncRelayCommand LoadStateCommand { get; }

    /// <summary>Gets the command to complete current step.</summary>
    public IAsyncRelayCommand CompleteStepCommand { get; }

    /// <summary>Gets the command to skip current step.</summary>
    public IAsyncRelayCommand SkipStepCommand { get; }

    /// <summary>Gets the command to reset wizard setup.</summary>
    public IAsyncRelayCommand ResetSetupCommand { get; }

    /// <summary>Gets the command to validate wizard setup.</summary>
    public IAsyncRelayCommand ValidateSetupCommand { get; }

    /// <summary>Gets the command to finalize wizard setup.</summary>
    public IAsyncRelayCommand FinalizeSetupCommand { get; }

    /// <summary>
    /// Initialises a new instance of the <see cref="CompanySetupDashboardViewModel"/> class.
    /// </summary>
    public CompanySetupDashboardViewModel(
        ISetupWorkflowService workflowService,
        ITenantProvider tenantProvider,
        IMediator mediator,
        INotificationService notificationService)
    {
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        InitializeChecklist();

        LoadStateCommand = new AsyncRelayCommand(LoadStateAsync);
        CompleteStepCommand = new AsyncRelayCommand(CompleteStepAsync);
        SkipStepCommand = new AsyncRelayCommand(SkipStepAsync);
        ResetSetupCommand = new AsyncRelayCommand(ResetSetupAsync);
        ValidateSetupCommand = new AsyncRelayCommand(ValidateSetupAsync);
        FinalizeSetupCommand = new AsyncRelayCommand(FinalizeSetupAsync);
    }

    private void InitializeChecklist()
    {
        Tasks.Clear();
        Tasks.Add(new SetupTaskItem(WizardStep.Welcome, "1. Welcome & Introduction", "Welcome to FEPS onboarding. Start the wizard."));
        Tasks.Add(new SetupTaskItem(WizardStep.CompanyDetails, "2. Company Details", "Configure legal/trading details, TIN, FNPF employer number."));
        Tasks.Add(new SetupTaskItem(WizardStep.FiscalCalendar, "3. Fiscal Calendar", "Generate financial years and periods."));
        Tasks.Add(new SetupTaskItem(WizardStep.PayrollFrequency, "4. Payroll Frequencies", "Configure payroll frequencies and schedules."));
        Tasks.Add(new SetupTaskItem(WizardStep.BankConfiguration, "5. Bank Accounts", "Set up corporate bank accounts."));
        Tasks.Add(new SetupTaskItem(WizardStep.Approvers, "6. Approvers & Routing", "Map routing workflows for payroll officer & supervisor roles."));
        Tasks.Add(new SetupTaskItem(WizardStep.Validation, "7. Validation Check", "Perform final verification checks before commit."));
    }

    private async Task LoadStateAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var state = await _workflowService.GetOrCreateSetupStateAsync(companyId);
            var tasks = await _workflowService.GetSetupTasksAsync(companyId);
            var company = await _workflowService.GetCompanyAsync(companyId);

            CurrentStep = state.CurrentStep;
            IsSetupComplete = state.IsCompleted;
            SetupCompletedUtc = company?.SetupCompletedUtc;

            foreach (var task in Tasks)
            {
                var matchingTask = tasks.FirstOrDefault(t => t.Step == task.Step);
                if (matchingTask != null)
                {
                    task.IsCompleted = matchingTask.Completed;
                    task.CompletedBy = matchingTask.CompletedBy;
                    task.CompletedUtc = matchingTask.CompletedUtc;
                }
                else
                {
                    task.IsCompleted = false;
                    task.CompletedBy = null;
                    task.CompletedUtc = null;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load setup state: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CompleteStepAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _workflowService.CompleteStepAsync(companyId, CurrentStep, "Admin", default);
            if (result.IsSuccess)
            {
                SuccessMessage = $"Step {CurrentStep} completed successfully.";
                _notificationService.Success(SuccessMessage, "Step Completed");
                await LoadStateAsync();
            }
            else
            {
                ErrorMessage = result.Error;
                _notificationService.Error(ErrorMessage ?? "Failed to complete step.", "Error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to complete step: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SkipStepAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _workflowService.SkipStepAsync(companyId, CurrentStep, default);
            if (result.IsSuccess)
            {
                SuccessMessage = $"Step {CurrentStep} skipped.";
                _notificationService.Success(SuccessMessage, "Step Skipped");
                await LoadStateAsync();
            }
            else
            {
                ErrorMessage = result.Error;
                _notificationService.Error(ErrorMessage ?? "Failed to skip step.", "Error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to skip step: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetSetupAsync()
    {
        var confirm = MessageBox.Show(
            "Are you sure you want to reset the wizard setup? This will clear all completed onboarding tasks and reset the state back to Welcome.",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _workflowService.ResetSetupAsync(companyId, default);
            if (result.IsSuccess)
            {
                SuccessMessage = "Setup wizard reset successfully.";
                _notificationService.Success(SuccessMessage, "Setup Reset");
                await LoadStateAsync();
            }
            else
            {
                ErrorMessage = result.Error;
                _notificationService.Error(ErrorMessage ?? "Failed to reset setup.", "Error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to reset setup: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ValidateSetupAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        ValidationErrors.Clear();
        ValidationWarnings.Clear();
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var queryResult = await _mediator.Send(new ValidateCompanyWizardQuery(companyId));
            if (queryResult.IsSuccess && queryResult.Value is not null)
            {
                var dto = queryResult.Value;
                IsValid = dto.IsValid;
                HasRunValidation = true;

                foreach (var err in dto.Errors)
                {
                    ValidationErrors.Add(err);
                }
                foreach (var warn in dto.Warnings)
                {
                    ValidationWarnings.Add(warn);
                }

                if (IsValid)
                {
                    SuccessMessage = "All validations passed. Ready to finalize wizard.";
                    _notificationService.Success(SuccessMessage, "Validation Succeeded");
                }
                else
                {
                    ErrorMessage = "Onboarding wizard validation has blocking errors.";
                    _notificationService.Warning(ErrorMessage, "Validation Failed");
                }
            }
            else
            {
                ErrorMessage = queryResult.Error ?? "Failed to run setup dry-run validation.";
                _notificationService.Error(ErrorMessage, "Error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to validate setup: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task FinalizeSetupAsync()
    {
        if (!IsValid)
        {
            MessageBox.Show("Please run validation and resolve all blocking errors before finalising onboarding.", "Validation Errors", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var confirm = MessageBox.Show(
            "Are you sure you want to finalise the wizard setup and lock company configuration? This action is irreversible.",
            "Confirm Finalization",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            Guid executionId = Guid.NewGuid();
            var commandResult = await _mediator.Send(new CreateCompanyWizardCommand(companyId, executionId));
            if (commandResult.IsSuccess)
            {
                SuccessMessage = "Company setup onboarding wizard finalized successfully!";
                _notificationService.Success(SuccessMessage, "Onboarding Setup Finalised");
                await LoadStateAsync();
            }
            else
            {
                ErrorMessage = commandResult.Error ?? "Failed to finalize company setup wizard.";
                _notificationService.Error(ErrorMessage, "Error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to finalize setup: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
