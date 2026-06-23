using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Compliance.Commands;
using FijiPayroll.Application.Features.Compliance.Queries;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel governing the compliance dashboard, simulations, validations, and bank file clearing controls.
/// </summary>
public sealed class ComplianceCenterViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    private string _activePeriodName = "Loading...";
    private string _activePeriodStatus = "N/A";
    private int _outstandingValidationErrorsCount;
    private ObservableCollection<SubmissionSummary> _recentSubmissions = new();

    private string _fnpfEeRateOverride = "0.08";
    private string _fnpfErRateOverride = "0.10";
    private string _payeThresholdOverride = "30000";
    private RuleSimulationEngine.RuleSimulationResult? _simulationResult;

    private ReconciliationVarianceModel? _reconciliationVariance;

    private CompliancePeriod? _selectedPeriod;
    private PayrollRunSummaryDto? _selectedRun;
    private ObservableCollection<CompliancePeriod> _periods = new();
    private ObservableCollection<PayrollRunSummaryDto> _runs = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceCenterViewModel"/> class.
    /// </summary>
    public ComplianceCenterViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));

        LoadDashboardCommand = new AsyncRelayCommand(LoadDashboardAsync);
        LockPeriodCommand = new AsyncRelayCommand(LockActivePeriodAsync);
        UnlockPeriodCommand = new AsyncRelayCommand(UnlockActivePeriodAsync);
        RunSimulationCommand = new AsyncRelayCommand(RunSimulationAsync);
        RunReconciliationCommand = new AsyncRelayCommand(RunReconciliationAsync);
        GenerateFileCommand = new AsyncRelayCommand<string>(GenerateFileAsync);
    }

    public string Title => "Statutory Compliance & Banking Center";

    // Dashboard Binding Properties
    public string ActivePeriodName
    {
        get => _activePeriodName;
        set => SetProperty(ref _activePeriodName, value);
    }

    public string ActivePeriodStatus
    {
        get => _activePeriodStatus;
        set => SetProperty(ref _activePeriodStatus, value);
    }

    public int OutstandingValidationErrorsCount
    {
        get => _outstandingValidationErrorsCount;
        set => SetProperty(ref _outstandingValidationErrorsCount, value);
    }

    public ObservableCollection<SubmissionSummary> RecentSubmissions
    {
        get => _recentSubmissions;
        set => SetProperty(ref _recentSubmissions, value);
    }

    // ComboBox Binding Properties
    public CompliancePeriod? SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnPropertyChanged(nameof(SelectedPeriodText));
                OnPropertyChanged(nameof(SelectedPeriodStatusText));
            }
        }
    }

    public PayrollRunSummaryDto? SelectedRun
    {
        get => _selectedRun;
        set => SetProperty(ref _selectedRun, value);
    }

    public ObservableCollection<CompliancePeriod> Periods
    {
        get => _periods;
        set => SetProperty(ref _periods, value);
    }

    public ObservableCollection<PayrollRunSummaryDto> Runs
    {
        get => _runs;
        set => SetProperty(ref _runs, value);
    }

    public string SelectedPeriodText => SelectedPeriod != null
        ? $"{new DateTime(SelectedPeriod.Year, SelectedPeriod.Month, 1):MMMM yyyy}"
        : "None Selected";

    public string SelectedPeriodStatusText => SelectedPeriod != null
        ? $"Status: {SelectedPeriod.Status}"
        : "Status: N/A";

    // Simulation Overrides Binding Properties
    public string FnpfEeRateOverride
    {
        get => _fnpfEeRateOverride;
        set => SetProperty(ref _fnpfEeRateOverride, value);
    }

    public string FnpfErRateOverride
    {
        get => _fnpfErRateOverride;
        set => SetProperty(ref _fnpfErRateOverride, value);
    }

    public string PayeThresholdOverride
    {
        get => _payeThresholdOverride;
        set => SetProperty(ref _payeThresholdOverride, value);
    }

    public RuleSimulationEngine.RuleSimulationResult? SimulationResult
    {
        get => _simulationResult;
        set => SetProperty(ref _simulationResult, value);
    }

    // Reconciliation Binding Properties
    public ReconciliationVarianceModel? ReconciliationVariance
    {
        get => _reconciliationVariance;
        set => SetProperty(ref _reconciliationVariance, value);
    }

    // Commands
    public ICommand LoadDashboardCommand { get; }
    public ICommand LockPeriodCommand { get; }
    public ICommand UnlockPeriodCommand { get; }
    public ICommand RunSimulationCommand { get; }
    public ICommand RunReconciliationCommand { get; }
    public ICommand GenerateFileCommand { get; }

    public async Task LoadDashboardAsync()
    {
        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            
            // 1. Load Dashboard Summary
            var res = await _mediator.Send(new GetComplianceDashboardQuery(companyId));
            if (res.IsSuccess && res.Value != null)
            {
                ActivePeriodName = res.Value.ActivePeriodName;
                ActivePeriodStatus = res.Value.ActivePeriodStatus;
                OutstandingValidationErrorsCount = res.Value.OutstandingValidationErrorsCount;

                RecentSubmissions.Clear();
                foreach (var sub in res.Value.RecentSubmissions)
                {
                    RecentSubmissions.Add(sub);
                }
            }

            // 2. Load Compliance Periods
            var periodsRes = await _mediator.Send(new GetCompliancePeriodsQuery(companyId));
            if (periodsRes.IsSuccess && periodsRes.Value != null)
            {
                Periods.Clear();
                foreach (var p in periodsRes.Value)
                {
                    Periods.Add(p);
                }

                if (SelectedPeriod == null && Periods.Count > 0)
                {
                    SelectedPeriod = Periods[0];
                }
            }

            // 3. Load Payroll Runs
            var runsRes = await _mediator.Send(new GetPayrollRunListQuery(companyId, null, null, 1, 100));
            if (runsRes.IsSuccess && runsRes.Value != null)
            {
                Runs.Clear();
                foreach (var r in runsRes.Value.Items)
                {
                    Runs.Add(r);
                }

                if (SelectedRun == null && Runs.Count > 0)
                {
                    SelectedRun = Runs[0];
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load compliance details: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LockActivePeriodAsync()
    {
        if (SelectedPeriod == null)
        {
            MessageBox.Show("Please select a compliance period first.", "Lock Period", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new LockPeriodCommand(SelectedPeriod.Id, true));
            if (res.IsSuccess)
            {
                await LoadDashboardAsync();
            }
            else
            {
                MessageBox.Show($"Failed to lock period: {res.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task UnlockActivePeriodAsync()
    {
        if (SelectedPeriod == null)
        {
            MessageBox.Show("Please select a compliance period first.", "Unlock Period", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new LockPeriodCommand(SelectedPeriod.Id, false));
            if (res.IsSuccess)
            {
                await LoadDashboardAsync();
            }
            else
            {
                MessageBox.Show($"Failed to unlock period: {res.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task RunSimulationAsync()
    {
        if (SelectedRun == null)
        {
            MessageBox.Show("Please select a payroll run first.", "Run Simulation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var overrides = new List<RuleSimulationEngine.RuleOverride>
            {
                new("FNPF_EE_RATE", FnpfEeRateOverride),
                new("FNPF_ER_RATE", FnpfErRateOverride),
                new("PAYE_TAX_FREE_THRESHOLD", PayeThresholdOverride)
            };

            var res = await _mediator.Send(new RunSimulationCommand(companyId, SelectedRun.Id, overrides));
            if (res.IsSuccess)
            {
                SimulationResult = res.Value;
            }
            else
            {
                MessageBox.Show($"Simulation failed: {res.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task RunReconciliationAsync()
    {
        if (SelectedRun == null)
        {
            MessageBox.Show("Please select a payroll run first.", "Run Reconciliation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new GetReconciliationVarianceQuery(SelectedRun.Id));
            if (res.IsSuccess)
            {
                ReconciliationVariance = res.Value;
            }
            else
            {
                MessageBox.Show($"Reconciliation failed: {res.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task GenerateFileAsync(string? jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType)) return;
        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var res = await _mediator.Send(new StartComplianceJobCommand(companyId, jobType));
            if (res.IsSuccess)
            {
                MessageBox.Show($"Compliance job '{jobType}' started successfully. Job ID: {res.Value}", "Job Started", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDashboardAsync();
            }
            else
            {
                MessageBox.Show($"Failed to start job: {res.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
