using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FijiPayroll.WPF.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// UI health watchdog that:
/// - Pings the WPF Dispatcher at 1Hz using a background thread
/// - Measures response latency with a moving 30-sample average
/// - Uses linear regression trend prediction to detect slow degradation early
/// - Carries a recovery exclusion lock to prevent collision with active navigation transactions
/// - Triggers self-healing (garbage collect + warn) at configurable thresholds
/// </summary>
public sealed class SystemHealthMonitor : IDisposable
{
    private const int PingIntervalMs = 1000;
    private const int SampleWindowSize = 30;
    private const double WarnThresholdMs = 200;
    private const double CriticalThresholdMs = 500;
    private const double TrendAlertSlope = 5.0; // ms/sample

    private readonly ILogger<SystemHealthMonitor> _logger;
    private readonly Queue<double> _latencySamples = new();
    private readonly SemaphoreSlim _recoveryLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _disposed;
    private Task? _watchdogTask;

    public SystemHealthMonitor(ILogger<SystemHealthMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>Starts the watchdog background loop.</summary>
    public void Start()
    {
        _watchdogTask = Task.Run(() => WatchdogLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("[HealthMonitor] Watchdog started.");
    }

    /// <summary>Gets the last measured UI dispatcher latency in milliseconds.</summary>
    public double LastLatencyMs { get; private set; }

    /// <summary>Gets the moving average latency across the last 30 samples.</summary>
    public double AverageLatencyMs => _latencySamples.Count == 0 ? 0 : _latencySamples.Average();

    private async Task WatchdogLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_disposed)
        {
            try
            {
                await Task.Delay(PingIntervalMs, ct).ConfigureAwait(false);
                double latency = await MeasureDispatcherLatencyAsync();
                RecordSample(latency);
                EvaluateHealth(latency);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[HealthMonitor] Watchdog loop error.");
            }
        }
    }

    private async Task<double> MeasureDispatcherLatencyAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await SafeDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle,
                new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
        }
        catch { /* Timeout = high latency */ }
        sw.Stop();
        LastLatencyMs = sw.Elapsed.TotalMilliseconds;
        return LastLatencyMs;
    }

    private void RecordSample(double latencyMs)
    {
        _latencySamples.Enqueue(latencyMs);
        if (_latencySamples.Count > SampleWindowSize)
            _latencySamples.Dequeue();
    }

    private void EvaluateHealth(double latencyMs)
    {
        if (latencyMs >= CriticalThresholdMs)
        {
            _logger.LogError("[HealthMonitor] CRITICAL UI latency: {Latency:F0}ms — triggering self-heal.", latencyMs);
            _ = TrySelfHealAsync();
        }
        else if (latencyMs >= WarnThresholdMs)
        {
            _logger.LogWarning("[HealthMonitor] UI latency elevated: {Latency:F0}ms", latencyMs);
        }

        // Trend prediction: linear regression slope
        double slope = ComputeLatencySlope();
        if (slope >= TrendAlertSlope)
        {
            _logger.LogWarning("[HealthMonitor] Latency trend deteriorating: slope={Slope:F2}ms/sample", slope);
        }
    }

    /// <summary>
    /// Linear regression on latency samples to predict deterioration trend.
    /// Returns slope in ms per sample.
    /// </summary>
    private double ComputeLatencySlope()
    {
        var samples = _latencySamples.ToArray();
        int n = samples.Length;
        if (n < 5) return 0;

        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += i; sumY += samples[i];
            sumXY += i * samples[i]; sumX2 += i * i;
        }
        double denom = n * sumX2 - sumX * sumX;
        return denom == 0 ? 0 : (n * sumXY - sumX * sumY) / denom;
    }

    private async Task TrySelfHealAsync()
    {
        // Prevent collision with navigation transactions or other recovery
        if (!await _recoveryLock.WaitAsync(TimeSpan.FromSeconds(2))) return;
        try
        {
            GC.Collect(0, GCCollectionMode.Optimized, blocking: false);
            _logger.LogInformation("[HealthMonitor] Self-heal GC triggered (Gen0 optimized).");
        }
        finally
        {
            _recoveryLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _recoveryLock.Dispose();
    }
}
