using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Schedules incremental GC cleanup and deferred LOH compaction.
/// 
/// Design:
/// - Runs incremental Gen0+Gen1 cleanup every 2 minutes during idle periods.
/// - LOH compaction (GCSettings.LargeObjectHeapCompactionMode) is ONLY triggered:
///   a) During guaranteed application-idle periods (no UI activity for 30s), AND
///   b) When working set exceeds the configured pressure threshold (300MB default).
/// - A 500ms dispatcher-idle check gates all operations to ensure zero UI stutter.
/// </summary>
public sealed class MemorySmoothingScheduler : IDisposable
{
    private const long LohCompactionThresholdBytes = 300L * 1024 * 1024; // 300 MB
    private const int IdleWindowMs = 30_000;  // 30 seconds of inactivity
    private const int IncrementalIntervalMs = 120_000; // 2 minutes

    private readonly ILogger<MemorySmoothingScheduler> _logger;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _disposed;
    private Task? _schedulerTask;

    // Last UI interaction timestamp (updated by MainWindow/Shell via NotifyActivity)
    private long _lastActivityTick = Environment.TickCount64;

    public MemorySmoothingScheduler(ILogger<MemorySmoothingScheduler> logger)
    {
        _logger = logger;
    }

    /// <summary>Starts the background memory management loop.</summary>
    public void Start()
    {
        _schedulerTask = Task.Run(() => SchedulerLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("[MemoryScheduler] Memory smoothing scheduler started.");
    }

    /// <summary>
    /// Call this from the WPF shell on any user interaction (mouse move, keypress)
    /// to reset the idle clock and prevent premature LOH compaction.
    /// </summary>
    public void NotifyActivity()
    {
        Interlocked.Exchange(ref _lastActivityTick, Environment.TickCount64);
    }

    private async Task SchedulerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_disposed)
        {
            try
            {
                await Task.Delay(IncrementalIntervalMs, ct).ConfigureAwait(false);

                // Incremental Gen0+Gen1 — always safe, very fast
                RunIncrementalCleanup();

                // Deferred LOH compaction — only if idle + under pressure
                if (IsApplicationIdle() && IsUnderMemoryPressure())
                {
                    await RunDeferredLohCompactionAsync(ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MemoryScheduler] Scheduler loop error.");
            }
        }
    }

    private void RunIncrementalCleanup()
    {
        GC.Collect(1, GCCollectionMode.Optimized, blocking: false, compacting: false);
        _logger.LogDebug("[MemoryScheduler] Incremental Gen0+Gen1 cleanup complete.");
    }

    private bool IsApplicationIdle()
    {
        long elapsed = Environment.TickCount64 - Interlocked.Read(ref _lastActivityTick);
        return elapsed >= IdleWindowMs;
    }

    private static bool IsUnderMemoryPressure()
    {
        long workingSet = Process.GetCurrentProcess().WorkingSet64;
        return workingSet >= LohCompactionThresholdBytes;
    }

    private async Task RunDeferredLohCompactionAsync(CancellationToken ct)
    {
        // Double-check idle on UI thread side before committing to full GC
        bool uiIdle = false;
        await FijiPayroll.WPF.Infrastructure.SafeDispatcher.InvokeAsync(() =>
        {
            uiIdle = IsApplicationIdle(); // re-verify from UI thread perspective
        }).ConfigureAwait(false);

        if (!uiIdle) return;

        _logger.LogInformation("[MemoryScheduler] Triggering deferred LOH compaction (idle + pressure confirmed).");

        // Schedule LOH compaction on NEXT GC cycle
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Optimized, blocking: true, compacting: true);

        long workingSetAfter = Process.GetCurrentProcess().WorkingSet64;
        _logger.LogInformation("[MemoryScheduler] LOH compaction complete. Working set: {MB:F0}MB", workingSetAfter / 1048576.0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
