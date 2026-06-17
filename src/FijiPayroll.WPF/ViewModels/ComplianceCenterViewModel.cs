using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Compliance.Commands;
using FijiPayroll.Application.Features.Compliance.Queries;
using FijiPayroll.Application.Services;
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
        }
        catch (Exception ex)
        {
            // Log or display error
            System.Diagnostics.Debug.WriteLine($"Failed to load dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LockActivePeriodAsync()
    {
        IsBusy = true;
        try
        {
            // Lock active period (hardcoded mock Period ID 1 for simplicity of cycle UI)
            await _mediator.Send(new LockPeriodCommand(1, true));
            await LoadDashboardAsync();
        }
        catch { }
        finally { IsBusy = false; }
    }

    private async Task UnlockActivePeriodAsync()
    {
        IsBusy = true;
        try
        {
            await _mediator.Send(new LockPeriodCommand(1, false));
            await LoadDashboardAsync();
        }
        catch { }
        finally { IsBusy = false; }
    }

    private async Task RunSimulationAsync()
    {
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

            // Hardcode payroll run ID 1 for simulation run demonstration
            var res = await _mediator.Send(new RunSimulationCommand(companyId, 1, overrides));
            if (res.IsSuccess)
            {
                SimulationResult = res.Value;
            }
        }
        catch { }
        finally { IsBusy = false; }
    }

    private async Task RunReconciliationAsync()
    {
        IsBusy = true;
        try
        {
            // Hardcode payroll run ID 1 for variance demonstration
            var res = await _mediator.Send(new GetReconciliationVarianceQuery(1));
            if (res.IsSuccess)
            {
                ReconciliationVariance = res.Value;
            }
        }
        catch { }
        finally { IsBusy = false; }
    }

    private async Task GenerateFileAsync(string? jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType)) return;
        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            // Start compliance background job
            await _mediator.Send(new StartComplianceJobCommand(companyId, jobType));
            // Reload list of recent files
            await Task.Delay(1000); // Wait slightly for background polling
            await LoadDashboardAsync();
        }
        catch { }
        finally { IsBusy = false; }
    }
}
