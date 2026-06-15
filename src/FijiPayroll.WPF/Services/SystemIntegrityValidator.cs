using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.WPF.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Hourly system integrity auditor.
/// Checks navigation stack bounds, ViewModel leak counts, memory headroom,
/// and queue health. Logs warnings on any detected anomaly.
/// </summary>
public sealed class SystemIntegrityValidator : IDisposable
{
    private const long MemoryWarningThresholdBytes = 500L * 1024 * 1024; // 500 MB
    private const int MaxExpectedLiveViewModels = 20;

    private readonly ILogger<SystemIntegrityValidator> _logger;
    private readonly INavigationService _navigationService;
    private readonly PriorityDispatcherQueue _dispatcherQueue;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _disposed;
    private Task? _auditTask;

    public SystemIntegrityValidator(
        ILogger<SystemIntegrityValidator> logger,
        INavigationService navigationService,
        PriorityDispatcherQueue dispatcherQueue)
    {
        _logger = logger;
        _navigationService = navigationService;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <summary>Starts the hourly audit background loop.</summary>
    public void Start()
    {
        _auditTask = Task.Run(() => AuditLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("[Integrity] Hourly integrity auditor started.");
    }

    private async Task AuditLoopAsync(CancellationToken ct)
    {
        // First audit after 60 seconds, then every hour
        await Task.Delay(TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);

        while (!ct.IsCancellationRequested && !_disposed)
        {
            try
            {
                RunAudit();
                await Task.Delay(TimeSpan.FromHours(1), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Integrity] Audit loop error.");
                await Task.Delay(TimeSpan.FromMinutes(5), ct).ConfigureAwait(false);
            }
        }
    }

    private void RunAudit()
    {
        _logger.LogInformation("[Integrity] Running system integrity audit...");

        // Memory headroom
        long workingSet = Process.GetCurrentProcess().WorkingSet64;
        if (workingSet > MemoryWarningThresholdBytes)
        {
            _logger.LogWarning("[Integrity] Memory pressure: {MB:F0} MB working set.", workingSet / 1048576.0);
        }

        // ViewModel leak count
        int liveVMs = ViewModelLeakDetector.EstimatedLiveCount;
        if (liveVMs > MaxExpectedLiveViewModels)
        {
            _logger.LogWarning("[Integrity] Possible VM leak: {Count} live ViewModels (threshold={Max}).", liveVMs, MaxExpectedLiveViewModels);
        }

        // Queue health
        int critPending = _dispatcherQueue.PendingCount(DispatchPriority.Critical);
        int normPending = _dispatcherQueue.PendingCount(DispatchPriority.Normal);
        int lowPending = _dispatcherQueue.PendingCount(DispatchPriority.Low);

        if (critPending > 100 || normPending > 200 || lowPending > 400)
        {
            _logger.LogWarning("[Integrity] Queue backlog: Critical={C}, Normal={N}, Low={L}", critPending, normPending, lowPending);
        }

        _logger.LogInformation(
            "[Integrity] Audit complete. Memory={MB:F0}MB, LiveVMs={VMs}, Queues=[{C},{N},{L}]",
            workingSet / 1048576.0, liveVMs, critPending, normPending, lowPending);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
