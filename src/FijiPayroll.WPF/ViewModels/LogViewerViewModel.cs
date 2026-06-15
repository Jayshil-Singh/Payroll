using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.ViewModels.Base;
using FijiPayroll.WPF.Services;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// Diagnostics Log Viewer ViewModel.
/// 
/// Features:
/// - 15ms batching timer slices all incoming log entries before updating the ObservableCollection.
/// - Hard cap of 1000 visible rows (oldest trimmed first).
/// - Diagnostics HUD properties: live ViewModel count, subscriptions estimate, and memory metrics.
/// - Full IDisposable lifecycle with EventSubscriptionTracker.
/// </summary>
public sealed class LogViewerViewModel : ViewModelBase
{
    // ─── Batching ─────────────────────────────────────────────────────────────
    private const int BatchIntervalMs = 15;
    private const int MaxVisibleRows = 1000;

    private readonly DispatcherTimer _batchTimer;
    private readonly List<LogEntry> _pendingBatch = new();
    private readonly object _batchLock = new();

    // ─── Dependencies ─────────────────────────────────────────────────────────
    private readonly ILogBuffer _logBuffer;

    // ─── Filter state ─────────────────────────────────────────────────────────
    private string _searchQuery = string.Empty;
    private bool _showInfo = true;
    private bool _showWarning = true;
    private bool _showError = true;

    // ─── HUD metrics ─────────────────────────────────────────────────────────
    private int _liveViewModelCount;
    private long _workingSetMb;
    private readonly DispatcherTimer _hudRefreshTimer;

    public LogViewerViewModel(ILogBuffer logBuffer)
    {
        _logBuffer = logBuffer;

        // 15ms batch flush timer
        _batchTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(BatchIntervalMs)
        };
        _batchTimer.Tick += FlushBatch;

        // HUD refresh every 2s
        _hudRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _hudRefreshTimer.Tick += RefreshHud;
        _hudRefreshTimer.Start();

        // Load initial ring-buffer snapshot
        RefreshLogs();

        // Subscribe via tracker so disposal is automatic
        Subscriptions.Track<LogEntry>(
            subscribe: h => _logBuffer.LogAdded += h,
            unsubscribe: h => _logBuffer.LogAdded -= h,
            handler: OnLogAdded);
    }

    // ─── Filter Properties ────────────────────────────────────────────────────

    public string SearchQuery
    {
        get => _searchQuery;
        set { if (SetProperty(ref _searchQuery, value)) RefreshLogs(); }
    }

    public bool ShowInfo
    {
        get => _showInfo;
        set { if (SetProperty(ref _showInfo, value)) RefreshLogs(); }
    }

    public bool ShowWarning
    {
        get => _showWarning;
        set { if (SetProperty(ref _showWarning, value)) RefreshLogs(); }
    }

    public bool ShowError
    {
        get => _showError;
        set { if (SetProperty(ref _showError, value)) RefreshLogs(); }
    }

    // ─── Log Collection ───────────────────────────────────────────────────────

    /// <summary>Filtered visible log entries — updated via 15ms batch timer.</summary>
    public ObservableCollection<LogEntry> FilteredLogs { get; } = new();

    // ─── Diagnostics HUD ─────────────────────────────────────────────────────

    /// <summary>Estimated number of live (not GC'd) ViewModels.</summary>
    public int LiveViewModelCount
    {
        get => _liveViewModelCount;
        private set => SetProperty(ref _liveViewModelCount, value);
    }

    /// <summary>Current process working set in megabytes.</summary>
    public long WorkingSetMb
    {
        get => _workingSetMb;
        private set => SetProperty(ref _workingSetMb, value);
    }

    // ─── Log Operations ───────────────────────────────────────────────────────

    /// <summary>Reloads the full filtered log list from the ring buffer.</summary>
    public void RefreshLogs()
    {
        var filtered = _logBuffer.GetLogs().Where(MatchesFilter).ToList();

        SafeDispatcher.SafeBeginInvoke(() =>
        {
            FilteredLogs.Clear();
            // Respect max row cap on initial load
            var capped = filtered.Count > MaxVisibleRows
                ? filtered.Skip(filtered.Count - MaxVisibleRows).ToList()
                : filtered;
            foreach (var log in capped)
                FilteredLogs.Add(log);
        });
    }

    private void OnLogAdded(LogEntry log)
    {
        if (!MatchesFilter(log)) return;

        lock (_batchLock)
        {
            _pendingBatch.Add(log);
        }

        // Ensure timer is running (must be done on UI thread)
        SafeDispatcher.SafeBeginInvoke(() =>
        {
            if (!_batchTimer.IsEnabled)
                _batchTimer.Start();
        }, DispatcherPriority.Normal);
    }

    private void FlushBatch(object? sender, EventArgs e)
    {
        _batchTimer.Stop();

        List<LogEntry> batch;
        lock (_batchLock)
        {
            if (_pendingBatch.Count == 0) return;
            batch = new List<LogEntry>(_pendingBatch);
            _pendingBatch.Clear();
        }

        foreach (var log in batch)
        {
            FilteredLogs.Add(log);
        }

        // Trim to max visible rows
        while (FilteredLogs.Count > MaxVisibleRows)
            FilteredLogs.RemoveAt(0);
    }

    private void RefreshHud(object? sender, EventArgs e)
    {
        LiveViewModelCount = ViewModelLeakDetector.EstimatedLiveCount;
        WorkingSetMb = Process.GetCurrentProcess().WorkingSet64 / 1_048_576;
    }

    private bool MatchesFilter(LogEntry log)
    {
        bool levelMatch = log.Level switch
        {
            LogLevel.Information => ShowInfo,
            LogLevel.Warning     => ShowWarning,
            LogLevel.Error       => ShowError,
            LogLevel.Critical    => ShowError,
            _                    => true
        };

        if (!levelMatch) return false;
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

        return log.Message.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
               log.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Disposal ─────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _batchTimer.Stop();
            _batchTimer.Tick -= FlushBatch;
            _hudRefreshTimer.Stop();
            _hudRefreshTimer.Tick -= RefreshHud;
        }
        base.Dispose(disposing);
    }
}
