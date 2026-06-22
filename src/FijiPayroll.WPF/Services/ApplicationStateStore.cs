using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.WPF.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Thread-safe, double-buffered Singleton implementation of IApplicationStateStore.
/// 
/// Features:
/// - All mutations are lock-protected with double-buffer swap.
/// - Freeze barrier (SnapshotCoordinator.IsFrozen) is respected: mutations are
///   queued and applied after the freeze lifts.
/// - Settings are serialized to disk atomically (temp-file swap) on every change.
/// - Restore validates values before applying.
/// </summary>
public sealed class ApplicationStateStore : IApplicationStateStore, IDisposable
{
    private readonly object _lock = new();
    private readonly ILogger<ApplicationStateStore>? _logger;
    private readonly string _settingsFilePath;

    // ─── Primary state buffer ─────────────────────────────────────────────────
    private ApplicationStateData _state = new();

    // ─── Double-buffer pending write ──────────────────────────────────────────
    private ApplicationStateData? _pendingState;

    // ─── Freeze guard timer ───────────────────────────────────────────────────
    private readonly System.Timers.Timer _freezeFlushTimer;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    public ApplicationStateStore(ILogger<ApplicationStateStore>? logger = null)
    {
        _logger = logger;
        _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FijiPayroll", "app_state.json");

        // Flush any pending freeze-queued state every 100ms
        _freezeFlushTimer = new System.Timers.Timer(100);
        _freezeFlushTimer.Elapsed += FlushPendingState;
        _freezeFlushTimer.AutoReset = true;
        _freezeFlushTimer.Start();
    }

    // ─── State Properties ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public int CurrentCompanyId
    {
        get { lock (_lock) return _state.CurrentCompanyId; }
        set => SetStateValue(s => s.CurrentCompanyId = value, nameof(CurrentCompanyId));
    }

    /// <inheritdoc />
    public int? CurrentPayrollRunId
    {
        get { lock (_lock) return _state.CurrentPayrollRunId; }
        set => SetStateValue(s => s.CurrentPayrollRunId = value, nameof(CurrentPayrollRunId));
    }

    /// <inheritdoc />
    public int SelectedFinancialYear
    {
        get { lock (_lock) return _state.SelectedFinancialYear; }
        set => SetStateValue(s => s.SelectedFinancialYear = value, nameof(SelectedFinancialYear));
    }

    /// <inheritdoc />
    public int? SelectedEmployeeId
    {
        get { lock (_lock) return _state.SelectedEmployeeId; }
        set => SetStateValue(s => s.SelectedEmployeeId = value, nameof(SelectedEmployeeId));
    }

    /// <inheritdoc />
    public bool RememberMe
    {
        get { lock (_lock) return _state.RememberMe; }
        set => SetStateValue(s => s.RememberMe = value, nameof(RememberMe));
    }

    /// <inheritdoc />
    public string RememberedUsername
    {
        get { lock (_lock) return _state.RememberedUsername; }
        set => SetStateValue(s => s.RememberedUsername = value, nameof(RememberedUsername));
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _state = new ApplicationStateData();
        }

        OnPropertyChanged(nameof(CurrentCompanyId));
        OnPropertyChanged(nameof(CurrentPayrollRunId));
        OnPropertyChanged(nameof(SelectedFinancialYear));
        OnPropertyChanged(nameof(SelectedEmployeeId));
        OnPropertyChanged(nameof(RememberMe));
        OnPropertyChanged(nameof(RememberedUsername));

        _ = PersistAsync(_state);
    }

    // ─── Snapshot Support ─────────────────────────────────────────────────────

    /// <summary>
    /// Takes an immutable snapshot of the current state.
    /// Respects the SnapshotCoordinator freeze barrier.
    /// </summary>
    public ApplicationStateData TakeSnapshot()
    {
        lock (_lock) return _state.Clone();
    }

    /// <summary>
    /// Restores state from a previously taken snapshot.
    /// Validates all values before applying.
    /// </summary>
    public void RestoreSnapshot(ApplicationStateData snapshot)
    {
        if (!ValidateSnapshot(snapshot))
        {
            _logger?.LogWarning("[StateStore] Snapshot validation failed — restore aborted.");
            return;
        }

        lock (_lock) { _state = snapshot.Clone(); }

        OnPropertyChanged(nameof(CurrentCompanyId));
        OnPropertyChanged(nameof(CurrentPayrollRunId));
        OnPropertyChanged(nameof(SelectedFinancialYear));
        OnPropertyChanged(nameof(SelectedEmployeeId));
    }

    // ─── Persistence ──────────────────────────────────────────────────────────

    /// <summary>
    /// Persists current state to disk atomically (write-to-temp then rename).
    /// </summary>
    public Task PersistCurrentStateAsync()
    {
        ApplicationStateData snapshot;
        lock (_lock) { snapshot = _state.Clone(); }
        return PersistAsync(snapshot);
    }

    private async Task PersistAsync(ApplicationStateData data)
    {
        try
        {
            string dir = Path.GetDirectoryName(_settingsFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string tempPath = _settingsFilePath + ".tmp";
            string json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);

            // Atomic swap
            File.Move(tempPath, _settingsFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[StateStore] Failed to persist state to disk.");
        }
    }

    /// <summary>Loads and restores persisted state from disk on startup.</summary>
    public async Task LoadPersistedStateAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath)) return;

            string json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<ApplicationStateData>(json);
            if (data != null && ValidateSnapshot(data))
            {
                lock (_lock) { _state = data; }
                _logger?.LogInformation("[StateStore] Persisted state restored successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[StateStore] Could not restore persisted state.");
        }
    }

    // ─── Internal Helpers ─────────────────────────────────────────────────────

    private void SetStateValue(Action<ApplicationStateData> mutate, string propertyName)
    {
        if (SnapshotCoordinator.IsFrozen)
        {
            // Queue mutation for after freeze lifts (double-buffer)
            lock (_lock)
            {
                _pendingState ??= _state.Clone();
                mutate(_pendingState);
            }
            return;
        }

        lock (_lock) { mutate(_state); }
        OnPropertyChanged(propertyName);
        _ = PersistAsync(_state);
    }

    private void FlushPendingState(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (SnapshotCoordinator.IsFrozen) return;

        ApplicationStateData? pending;
        lock (_lock)
        {
            if (_pendingState == null) return;
            pending = _pendingState;
            _state = pending;
            _pendingState = null;
        }

        OnPropertyChanged(nameof(CurrentCompanyId));
        OnPropertyChanged(nameof(CurrentPayrollRunId));
        OnPropertyChanged(nameof(SelectedFinancialYear));
        OnPropertyChanged(nameof(SelectedEmployeeId));
        OnPropertyChanged(nameof(RememberMe));
        OnPropertyChanged(nameof(RememberedUsername));
        _ = PersistAsync(pending);
    }

    private static bool ValidateSnapshot(ApplicationStateData data)
    {
        if (data.CurrentCompanyId <= 0) return false;
        if (data.SelectedFinancialYear < 2000 || data.SelectedFinancialYear > DateTime.Now.Year + 5) return false;
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return;
        }

        var dispatcher = app.Dispatcher;
        if (dispatcher == null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return;
        }

        // Thread safety: Avoid cross-thread exceptions in WPF binding engine
        if (dispatcher.CheckAccess())
        {
            // Invoke directly if already on UI thread to bypass scheduling queue overhead
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        else
        {
            // Fall back to non-blocking asynchronous BeginInvoke to keep background thread processing free of deadlocks
            try
            {
                if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    }));
                }
            }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
    }

    public void Dispose()
    {
        _freezeFlushTimer.Stop();
        _freezeFlushTimer.Dispose();
    }
}

/// <summary>Serializable state data bag — value type semantics via explicit Clone().</summary>
public sealed class ApplicationStateData
{
    public int CurrentCompanyId { get; set; } = 1;
    public int? CurrentPayrollRunId { get; set; }
    public int SelectedFinancialYear { get; set; } = DateTime.Now.Year;
    public int? SelectedEmployeeId { get; set; }
    public bool RememberMe { get; set; }
    public string RememberedUsername { get; set; } = string.Empty;

    public ApplicationStateData Clone() => new()
    {
        CurrentCompanyId = CurrentCompanyId,
        CurrentPayrollRunId = CurrentPayrollRunId,
        SelectedFinancialYear = SelectedFinancialYear,
        SelectedEmployeeId = SelectedEmployeeId,
        RememberMe = RememberMe,
        RememberedUsername = RememberedUsername,
    };
}
