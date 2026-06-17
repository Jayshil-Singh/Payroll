using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Features.Compliance.Queries;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel governing the diagnostics and system latencies HUD.
/// </summary>
public sealed class DiagnosticsDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly DispatcherTimer _refreshTimer;

    private int _healthScore = 100;
    private long _databaseLatencyMs;
    private double _memoryUsedMb;
    private int _busyWorkerThreads;
    private int _activeWorkerThreads;
    private int _pendingJobsCount;
    private int _pendingNotificationsCount;
    private double _cpuUsagePercent;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsDashboardViewModel"/> class.
    /// </summary>
    public DiagnosticsDashboardViewModel(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        
        RefreshCommand = new AsyncRelayCommand(RefreshDiagnosticsAsync);

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshDiagnosticsAsync();
    }

    public string Title => "System Performance & Diagnostics HUD";

    // Bindable Diagnostic metrics
    public int HealthScore
    {
        get => _healthScore;
        set => SetProperty(ref _healthScore, value);
    }

    public long DatabaseLatencyMs
    {
        get => _databaseLatencyMs;
        set => SetProperty(ref _databaseLatencyMs, value);
    }

    public double MemoryUsedMb
    {
        get => _memoryUsedMb;
        set => SetProperty(ref _memoryUsedMb, value);
    }

    public int BusyWorkerThreads
    {
        get => _busyWorkerThreads;
        set => SetProperty(ref _busyWorkerThreads, value);
    }

    public int ActiveWorkerThreads
    {
        get => _activeWorkerThreads;
        set => SetProperty(ref _activeWorkerThreads, value);
    }

    public int PendingJobsCount
    {
        get => _pendingJobsCount;
        set => SetProperty(ref _pendingJobsCount, value);
    }

    public int PendingNotificationsCount
    {
        get => _pendingNotificationsCount;
        set => SetProperty(ref _pendingNotificationsCount, value);
    }

    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        set => SetProperty(ref _cpuUsagePercent, value);
    }

    public ICommand RefreshCommand { get; }

    /// <summary>Starts periodic refresh of diagnostic statistics.</summary>
    public void StartMonitoring()
    {
        _refreshTimer.Start();
        _ = RefreshDiagnosticsAsync();
    }

    /// <summary>Stops periodic refresh of diagnostic statistics.</summary>
    public void StopMonitoring()
    {
        _refreshTimer.Stop();
    }

    private async Task RefreshDiagnosticsAsync()
    {
        try
        {
            var res = await _mediator.Send(new GetDiagnosticsInfoQuery());
            if (res.IsSuccess && res.Value != null)
            {
                HealthScore = res.Value.HealthScore;
                DatabaseLatencyMs = res.Value.DatabaseLatencyMs;
                MemoryUsedMb = res.Value.MemoryUsedMb;
                BusyWorkerThreads = res.Value.BusyWorkerThreads;
                ActiveWorkerThreads = res.Value.ActiveWorkerThreads;
                PendingJobsCount = res.Value.PendingJobsCount;
                PendingNotificationsCount = res.Value.PendingNotificationsCount;
                CpuUsagePercent = res.Value.CpuUsagePercent;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to refresh diagnostics: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
        }
        base.Dispose(disposing);
    }
}
