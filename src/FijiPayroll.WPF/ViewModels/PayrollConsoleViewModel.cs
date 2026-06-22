using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Application.Features.PayrollAdjustments.Commands.StagePayrollAdjustment;
using FijiPayroll.Application.Features.PayrollGroups.Commands.CreatePayrollGroup;
using FijiPayroll.Application.Features.PayrollGroups.Queries.GetPayrollGroups;
using FijiPayroll.Application.Features.PayrollPeriods.Commands.ClosePayrollPeriod;
using FijiPayroll.Application.Features.PayrollPeriods.Commands.CreatePayrollPeriod;
using FijiPayroll.Application.Features.PayrollPeriods.Commands.LockPayrollPeriod;
using FijiPayroll.Application.Features.PayrollPeriods.Queries.GetPayrollPeriods;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ApprovePayrollRunWithSignature;
using FijiPayroll.Application.Features.PayrollRuns.Commands.FreezeLedger;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ProcessBatchPayroll;
using FijiPayroll.Application.Features.PayrollRuns.Commands.RollbackPayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Queries.ComparePayrollRuns;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetExceptions;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetLedger;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollHistory;
using FijiPayroll.Application.Features.Compliance.Queries;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace FijiPayroll.WPF.ViewModels;

public sealed class PayrollConsoleViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly BatchProcessingCoordinator _coordinator;
    private readonly INotificationService _notifications;
    private readonly DispatcherTimer _telemetryTimer;
    private readonly PayrollReplayEngine _replayEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private CancellationTokenSource? _batchCts;

    // Overview & Selection
    private int _companyId = 1; // Standard default company
    private int _selectedRunId;
    private PayrollPeriod? _selectedPeriod;

    // Period Manager
    private ObservableCollection<PayrollPeriod> _periods = [];
    private string _newPeriodCode = string.Empty;
    private PayrollFrequencyType _newPeriodFrequency = PayrollFrequencyType.Monthly;
    private DateTime _newStartDate = DateTime.UtcNow.Date;
    private DateTime _newEndDate = DateTime.UtcNow.AddMonths(1).Date;
    private DateTime _newPaymentDate = DateTime.UtcNow.AddMonths(1).Date;

    // Payroll Groups
    private ObservableCollection<PayrollGroup> _groups = [];
    private string _newGroupName = string.Empty;
    private string _newGroupCode = string.Empty;

    // Adjustments
    private int _adjEmployeeId = 1;
    private PayrollAdjustmentType _adjType = PayrollAdjustmentType.Earning;
    private decimal _adjAmount;
    private string _adjDescription = string.Empty;

    // Batch Monitor
    private double _batchProgress;
    private bool _isBatchRunning;
    private bool _isBatchPaused;
    private string _batchStatusText = "Idle";
    private DateTime _batchStartTime;
    private string _elapsedTimeText = "00:00:00";

    // Exceptions
    private ObservableCollection<PayrollExceptionQueue> _exceptions = [];
    private PayrollExceptionQueue? _selectedException;
    private string _resolutionText = string.Empty;

    // Ledger Explorer
    private PayrollLedger? _activeLedger;
    private ObservableCollection<PayrollLedgerEmployee> _ledgerEmployees = [];
    private ObservableCollection<PayrollLedgerTransaction> _ledgerTransactions = [];

    // Difference Explorer
    private int _compareRunAId;
    private int _compareRunBId;
    private PayrollDifferenceReport? _differenceReport;

    // Replay Console
    private string _replayStatusText = "Idle";
    private bool? _replayMatchResult;
    private string _replayCalculatedHash = string.Empty;

    // Real-Time Telemetry
    private double _cpuUsage;
    private double _memoryUsageMb;
    private int _threadCount;
    private int _gcCollections;
    private string _systemThroughput = "0 items/sec";

    public PayrollConsoleViewModel(
        IMediator mediator,
        BatchProcessingCoordinator coordinator,
        INotificationService notifications,
        PayrollReplayEngine replayEngine,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _replayEngine = replayEngine ?? throw new ArgumentNullException(nameof(replayEngine));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

        // Command Registrations
        LoadPeriodDataCommand = new AsyncRelayCommand(LoadPeriodsAsync);
        CreatePeriodCommand = new AsyncRelayCommand(CreatePeriodAsync);
        ClosePeriodCommand = new AsyncRelayCommand(ClosePeriodAsync);
        LockPeriodCommand = new AsyncRelayCommand(LockPeriodAsync);

        LoadGroupDataCommand = new AsyncRelayCommand(LoadGroupsAsync);
        CreateGroupCommand = new AsyncRelayCommand(CreateGroupAsync);

        StageAdjustmentCommand = new AsyncRelayCommand(StageAdjustmentAsync);

        StartBatchCommand = new AsyncRelayCommand(StartBatchAsync, () => !_isBatchRunning);
        PauseBatchCommand = new RelayCommand(PauseBatch, () => _isBatchRunning && !_isBatchPaused);
        ResumeBatchCommand = new RelayCommand(ResumeBatch, () => _isBatchRunning && _isBatchPaused);
        CancelBatchCommand = new RelayCommand(CancelBatch, () => _isBatchRunning);

        LoadExceptionsCommand = new AsyncRelayCommand(LoadExceptionsAsync);
        ResolveExceptionCommand = new AsyncRelayCommand(ResolveExceptionAsync, () => _selectedException != null);

        LoadLedgerCommand = new AsyncRelayCommand(LoadLedgerAsync);
        CompareRunsCommand = new AsyncRelayCommand(CompareRunsAsync);

        RunReplayCommand = new AsyncRelayCommand(RunReplayAsync);

        // Telemetry Refresh Timer (1s intervals)
        _telemetryTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _telemetryTimer.Tick += async (s, e) => await RefreshTelemetryAsync();
        _telemetryTimer.Start();

        // Initial loads
        _ = LoadPeriodsAsync();
        _ = LoadGroupsAsync();
    }

    // Bindable Properties
    public int CompanyId { get => _companyId; set => SetProperty(ref _companyId, value); }
    public int SelectedRunId { get => _selectedRunId; set => SetProperty(ref _selectedRunId, value); }
    public PayrollPeriod? SelectedPeriod { get => _selectedPeriod; set => SetProperty(ref _selectedPeriod, value); }

    // Period Manager list
    public ObservableCollection<PayrollPeriod> Periods => _periods;
    public string NewPeriodCode { get => _newPeriodCode; set => SetProperty(ref _newPeriodCode, value); }
    public PayrollFrequencyType NewPeriodFrequency { get => _newPeriodFrequency; set => SetProperty(ref _newPeriodFrequency, value); }
    public DateTime NewStartDate { get => _newStartDate; set => SetProperty(ref _newStartDate, value); }
    public DateTime NewEndDate { get => _newEndDate; set => SetProperty(ref _newEndDate, value); }
    public DateTime NewPaymentDate { get => _newPaymentDate; set => SetProperty(ref _newPaymentDate, value); }

    // Group Setup properties
    public ObservableCollection<PayrollGroup> Groups => _groups;
    public string NewGroupName { get => _newGroupName; set => SetProperty(ref _newGroupName, value); }
    public string NewGroupCode { get => _newGroupCode; set => SetProperty(ref _newGroupCode, value); }

    // Adjustment properties
    public int AdjEmployeeId { get => _adjEmployeeId; set => SetProperty(ref _adjEmployeeId, value); }
    public PayrollAdjustmentType AdjType { get => _adjType; set => SetProperty(ref _adjType, value); }
    public decimal AdjAmount { get => _adjAmount; set => SetProperty(ref _adjAmount, value); }
    public string AdjDescription { get => _adjDescription; set => SetProperty(ref _adjDescription, value); }

    // Batch Monitor properties
    public double BatchProgress { get => _batchProgress; set => SetProperty(ref _batchProgress, value); }
    public bool IsBatchRunning 
    { 
        get => _isBatchRunning; 
        set 
        { 
            if (SetProperty(ref _isBatchRunning, value))
            {
                (StartBatchCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (PauseBatchCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (ResumeBatchCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (CancelBatchCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        } 
    }
    public bool IsBatchPaused 
    { 
        get => _isBatchPaused; 
        set 
        { 
            if (SetProperty(ref _isBatchPaused, value))
            {
                (PauseBatchCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (ResumeBatchCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        } 
    }
    public string BatchStatusText { get => _batchStatusText; set => SetProperty(ref _batchStatusText, value); }
    public string ElapsedTimeText { get => _elapsedTimeText; set => SetProperty(ref _elapsedTimeText, value); }

    // Exceptions properties
    public ObservableCollection<PayrollExceptionQueue> Exceptions => _exceptions;
    public PayrollExceptionQueue? SelectedException 
    { 
        get => _selectedException; 
        set 
        { 
            if (SetProperty(ref _selectedException, value))
            {
                (ResolveExceptionCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        } 
    }
    public string ResolutionText { get => _resolutionText; set => SetProperty(ref _resolutionText, value); }

    // Ledger properties
    public PayrollLedger? ActiveLedger { get => _activeLedger; set => SetProperty(ref _activeLedger, value); }
    public ObservableCollection<PayrollLedgerEmployee> LedgerEmployees => _ledgerEmployees;
    public ObservableCollection<PayrollLedgerTransaction> LedgerTransactions => _ledgerTransactions;

    // Difference Explorer properties
    public int CompareRunAId { get => _compareRunAId; set => SetProperty(ref _compareRunAId, value); }
    public int CompareRunBId { get => _compareRunBId; set => SetProperty(ref _compareRunBId, value); }
    public PayrollDifferenceReport? DifferenceReport { get => _differenceReport; set => SetProperty(ref _differenceReport, value); }

    // Replay Console properties
    public string ReplayStatusText { get => _replayStatusText; set => SetProperty(ref _replayStatusText, value); }
    public bool? ReplayMatchResult { get => _replayMatchResult; set => SetProperty(ref _replayMatchResult, value); }
    public string ReplayCalculatedHash { get => _replayCalculatedHash; set => SetProperty(ref _replayCalculatedHash, value); }

    // Telemetry properties
    public double CpuUsage { get => _cpuUsage; set => SetProperty(ref _cpuUsage, value); }
    public double MemoryUsageMb { get => _memoryUsageMb; set => SetProperty(ref _memoryUsageMb, value); }
    public int ThreadCount { get => _threadCount; set => SetProperty(ref _threadCount, value); }
    public int GcCollections { get => _gcCollections; set => SetProperty(ref _gcCollections, value); }
    public string SystemThroughput { get => _systemThroughput; set => SetProperty(ref _systemThroughput, value); }

    // Commands
    public ICommand LoadPeriodDataCommand { get; }
    public ICommand CreatePeriodCommand { get; }
    public ICommand ClosePeriodCommand { get; }
    public ICommand LockPeriodCommand { get; }

    public ICommand LoadGroupDataCommand { get; }
    public ICommand CreateGroupCommand { get; }

    public ICommand StageAdjustmentCommand { get; }

    public ICommand StartBatchCommand { get; }
    public ICommand PauseBatchCommand { get; }
    public ICommand ResumeBatchCommand { get; }
    public ICommand CancelBatchCommand { get; }

    public ICommand LoadExceptionsCommand { get; }
    public ICommand ResolveExceptionCommand { get; }

    public ICommand LoadLedgerCommand { get; }
    public ICommand CompareRunsCommand { get; }

    public ICommand RunReplayCommand { get; }

    // Logic Implementations

    private async Task LoadPeriodsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new GetPayrollPeriodsQuery(CompanyId));
            if (res.IsSuccess)
            {
                Periods.Clear();
                foreach (var p in res.Value) Periods.Add(p);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Failed to load payroll periods.", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreatePeriodAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPeriodCode))
        {
            _notifications.ShowWarning("Validation Error", "Period code is required.");
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new CreatePayrollPeriodCommand(
                CompanyId, NewPeriodCode, NewPeriodFrequency, 2026, 6, NewStartDate, NewEndDate, NewPaymentDate));

            if (res.IsSuccess)
            {
                _notifications.ShowSuccess("Success", $"Created period {NewPeriodCode} successfully.");
                NewPeriodCode = string.Empty;
                await LoadPeriodsAsync();
            }
            else
            {
                _notifications.ShowError("Error creating period", res.Error);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Execution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClosePeriodAsync()
    {
        if (SelectedPeriod == null) return;
        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new ClosePayrollPeriodCommand(SelectedPeriod.Id));
            if (res.IsSuccess)
            {
                _notifications.ShowSuccess("Success", "Closed period successfully.");
                await LoadPeriodsAsync();
            }
            else
            {
                _notifications.ShowError("Closure Blocked", res.Error);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Execution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LockPeriodAsync()
    {
        if (SelectedPeriod == null) return;
        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new LockPayrollPeriodCommand(SelectedPeriod.Id));
            if (res.IsSuccess)
            {
                _notifications.ShowSuccess("Success", "Locked period successfully.");
                await LoadPeriodsAsync();
            }
            else
            {
                _notifications.ShowError("Locking Blocked", res.Error);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Execution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadGroupsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new GetPayrollGroupsQuery(CompanyId));
            if (res.IsSuccess)
            {
                Groups.Clear();
                foreach (var g in res.Value) Groups.Add(g);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Failed to load groups", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName) || string.IsNullOrWhiteSpace(NewGroupCode))
        {
            _notifications.ShowWarning("Validation Error", "Group name and code are required.");
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new CreatePayrollGroupCommand(
                CompanyId, NewGroupName, NewGroupCode, null, null, null, "HQ", null, null));

            if (res.IsSuccess)
            {
                _notifications.ShowSuccess("Success", $"Created payroll group {NewGroupName} successfully.");
                NewGroupName = string.Empty;
                NewGroupCode = string.Empty;
                await LoadGroupsAsync();
            }
            else
            {
                _notifications.ShowError("Error creating group", res.Error);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Execution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StageAdjustmentAsync()
    {
        if (AdjAmount == 0)
        {
            _notifications.ShowWarning("Validation Error", "Amount cannot be zero.");
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new StagePayrollAdjustmentCommand(
                CompanyId, AdjEmployeeId, AdjType, AdjAmount, AdjDescription));

            if (res.IsSuccess)
            {
                _notifications.ShowSuccess("Success", "Manual adjustment staged.");
                AdjAmount = 0;
                AdjDescription = string.Empty;
            }
            else
            {
                _notifications.ShowError("Error staging adjustment", res.Error);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Execution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Batch monitor execution
    private async Task StartBatchAsync()
    {
        if (SelectedRunId <= 0)
        {
            _notifications.ShowWarning("Validation Warning", "Select a valid Payroll Run ID to calculate.");
            return;
        }

        IsBatchRunning = true;
        IsBatchPaused = false;
        BatchProgress = 0;
        BatchStatusText = "Initializing...";
        _batchStartTime = DateTime.UtcNow;
        _batchCts = new CancellationTokenSource();

        // Start progress tracking loop
        var state = _coordinator.GetOrCreateState(SelectedRunId);
        _ = Task.Run(async () =>
        {
            while (IsBatchRunning && !_batchCts.IsCancellationRequested)
            {
                await Task.Delay(200);
                SafeDispatcher.Invoke(() =>
                {
                    BatchProgress = state.Progress;
                    BatchStatusText = IsBatchPaused ? "Paused" : $"Processing... {BatchProgress}%";
                    var elapsed = DateTime.UtcNow - _batchStartTime;
                    ElapsedTimeText = elapsed.ToString(@"hh\:mm\:ss");
                });
            }
        });

        try
        {
            var res = await _mediator.Send(new ProcessBatchPayrollCommand(SelectedRunId, Guid.NewGuid()), _batchCts.Token);
            if (res.IsSuccess)
            {
                _notifications.ShowSuccess("Calculations Completed", "Batch calculations completed successfully.");
                BatchStatusText = "Completed";
            }
            else
            {
                _notifications.ShowError("Batch Processing Failed", res.Error);
                BatchStatusText = "Failed";
            }
        }
        catch (OperationCanceledException)
        {
            _notifications.ShowWarning("Aborted", "Batch calculations cancelled by operator.");
            BatchStatusText = "Cancelled";
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Critical Exception in Batch", ex.Message);
            BatchStatusText = "Failed";
        }
        finally
        {
            IsBatchRunning = false;
            _batchCts?.Dispose();
            _batchCts = null;
        }
    }

    private void PauseBatch()
    {
        if (!IsBatchRunning || IsBatchPaused) return;
        var state = _coordinator.GetOrCreateState(SelectedRunId);
        state.IsPaused = true;
        IsBatchPaused = true;
    }

    private void ResumeBatch()
    {
        if (!IsBatchRunning || !IsBatchPaused) return;
        var state = _coordinator.GetOrCreateState(SelectedRunId);
        state.IsPaused = false;
        IsBatchPaused = false;
    }

    private void CancelBatch()
    {
        _batchCts?.Cancel();
    }

    // Exceptions Console
    private async Task LoadExceptionsAsync()
    {
        if (SelectedRunId <= 0) return;
        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new GetExceptionsQuery(SelectedRunId));
            if (res.IsSuccess)
            {
                Exceptions.Clear();
                foreach (var ex in res.Value) Exceptions.Add(ex);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Failed to load exception queue.", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResolveExceptionAsync()
    {
        if (SelectedException == null || string.IsNullOrWhiteSpace(ResolutionText))
        {
            _notifications.ShowWarning("Validation Warning", "Resolution statement cannot be blank.");
            return;
        }

        IsBusy = true;
        try
        {
            // Update entity in db directly for simplicity
            SelectedException.Resolve(ResolutionText, _currentUser?.Username ?? "Operator");
            _unitOfWork.PayrollExceptionQueues.Update(SelectedException);
            await _unitOfWork.SaveChangesAsync();

            _notifications.ShowSuccess("Resolved", $"Exception resolved for {SelectedException.EmployeeName}.");
            ResolutionText = string.Empty;
            await LoadExceptionsAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Resolution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Ledger Explorer
    private async Task LoadLedgerAsync()
    {
        if (SelectedRunId <= 0) return;
        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new GetLedgerQuery(SelectedRunId));
            if (res.IsSuccess)
            {
                ActiveLedger = res.Value;
                LedgerEmployees.Clear();
                foreach (var emp in res.Value.Employees) LedgerEmployees.Add(emp);

                LedgerTransactions.Clear();
                foreach (var tx in res.Value.Transactions) LedgerTransactions.Add(tx);
            }
            else
            {
                _notifications.ShowWarning("No Ledger Header", res.Error);
                ActiveLedger = null;
                LedgerEmployees.Clear();
                LedgerTransactions.Clear();
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Failed to retrieve ledger.", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Difference Explorer
    private async Task CompareRunsAsync()
    {
        if (CompareRunAId <= 0 || CompareRunBId <= 0)
        {
            _notifications.ShowWarning("Validation Error", "Please specify Run A and Run B IDs.");
            return;
        }

        IsBusy = true;
        try
        {
            var res = await _mediator.Send(new ComparePayrollRunsQuery(CompareRunAId, CompareRunBId));
            if (res.IsSuccess)
            {
                DifferenceReport = res.Value;
            }
            else
            {
                _notifications.ShowError("Comparison Error", res.Error);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Execution Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Replay Console
    private async Task RunReplayAsync()
    {
        if (SelectedRunId <= 0)
        {
            _notifications.ShowWarning("Validation Warning", "Select a Run ID first.");
            return;
        }

        IsBusy = true;
        ReplayStatusText = "Loading frozen snapshot...";
        try
        {
            var snapshot = await _unitOfWork.PayrollSnapshots.GetLatestByRunIdAsync(SelectedRunId);
            if (snapshot == null)
            {
                ReplayStatusText = "Failure: Stored calculation snapshot not found.";
                _notifications.ShowError("Replay Error", "No frozen snapshots exist for this payroll run.");
                return;
            }

            ReplayStatusText = "Re-evaluating context calculations...";
            bool isMatched = _replayEngine.Replay(
                snapshot,
                out string calcHash,
                out decimal gross,
                out decimal paye,
                out decimal net);

            ReplayMatchResult = isMatched;
            ReplayCalculatedHash = calcHash;
            ReplayStatusText = isMatched ? "Replay PASSED" : "Replay FAILED (TAMP_WARN)";

            if (isMatched)
            {
                _notifications.ShowSuccess("Replay Success", "Recalculated hashes match stored snapshot exactly.");
            }
            else
            {
                _notifications.ShowWarning("TAMP_WARN", "Recalculated totals do not match stored snapshots! Integrity compromised!");
            }
        }
        catch (Exception ex)
        {
            ReplayStatusText = "Replay Exception";
            _notifications.ShowError("Replay Failure", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Telemetry Dashboard
    private async Task RefreshTelemetryAsync()
    {
        try
        {
            var res = await _mediator.Send(new GetDiagnosticsInfoQuery());
            if (res.IsSuccess && res.Value != null)
            {
                CpuUsage = res.Value.CpuUsagePercent;
                MemoryUsageMb = res.Value.MemoryUsedMb;
                ThreadCount = res.Value.ActiveWorkerThreads;
                GcCollections = GC.CollectionCount(0);
                SystemThroughput = $"{res.Value.DatabaseLatencyMs} ms DB Latency";
            }
        }
        catch
        {
            // Suppress background tick telemetry errors to keep logs clean
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _telemetryTimer.Stop();
            _batchCts?.Cancel();
        }
        base.Dispose(disposing);
    }
}

internal static class NotificationServiceExtensions
{
    public static void ShowSuccess(this INotificationService service, string title, string message)
    {
        service.Success(message, title);
    }

    public static void ShowWarning(this INotificationService service, string title, string message)
    {
        service.Warning(message, title);
    }

    public static void ShowError(this INotificationService service, string title, string message)
    {
        service.Error(message, title);
    }
}
