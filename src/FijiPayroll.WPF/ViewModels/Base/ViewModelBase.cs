using System;
using System.Threading;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FijiPayroll.WPF.Infrastructure;

namespace FijiPayroll.WPF.ViewModels.Base;

/// <summary>
/// Base view model providing:
/// - INotifyPropertyChanged via ObservableObject (CommunityToolkit.Mvvm)
/// - 30Hz throttled bounded UI update dispatcher
/// - EventSubscriptionTracker for safe handler detachment
/// - ViewModelLeakDetector registration (DEBUG only)
/// - Strict IDisposable lifecycle with finalizer fallback
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    // ─── UI Update Throttle ───────────────────────────────────────────────────
    // 30 Hz = ~33ms between frames
    private const int ThrottleIntervalMs = 33;

    private readonly DispatcherTimer _throttleTimer;
    private volatile bool _pendingRefresh;

    // ─── Bounded Update Queue ─────────────────────────────────────────────────
    /// <summary>
    /// Bounded UI update queue (500 items, DropOldest policy).
    /// Subclasses enqueue string property names here; the throttle timer drains them.
    /// </summary>
    protected readonly UIUpdateQueue<string> PropertyUpdateQueue;

    // ─── Subscription Tracking ────────────────────────────────────────────────
    /// <summary>
    /// Tracks all event subscriptions. Call <see cref="Subscriptions"/>.Track(...)
    /// instead of manually subscribing to events.
    /// </summary>
    protected readonly EventSubscriptionTracker Subscriptions = new();

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private bool _isBusy;
    private volatile bool _disposed;

    protected ViewModelBase()
    {
        // Register for DEBUG leak tracking
        ViewModelLeakDetector.StaticRegister(this);

        // Bounded update queue — DropOldest keeps freshest data
        PropertyUpdateQueue = new UIUpdateQueue<string>(
            maxCapacity: 500,
            policy: QueueOverflowPolicy.DropOldest);

        // 30 Hz throttle timer — fires on UI thread
        _throttleTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(ThrottleIntervalMs)
        };
        _throttleTimer.Tick += OnThrottleTick;
    }

    // ─── IsBusy ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether the view model is performing a background operation.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    // ─── Throttled Dispatch ───────────────────────────────────────────────────

    /// <summary>
    /// Schedules a property change notification via the 30 Hz throttle timer.
    /// Safe to call from any thread.
    /// </summary>
    protected void ScheduleUpdate(string propertyName)
    {
        if (_disposed) return;

        PropertyUpdateQueue.Enqueue(propertyName);

        if (!_pendingRefresh)
        {
            _pendingRefresh = true;
            SafeDispatcher.SafeBeginInvoke(() => _throttleTimer.Start(), DispatcherPriority.Normal);
        }
    }

    private void OnThrottleTick(object? sender, EventArgs e)
    {
        _throttleTimer.Stop();
        _pendingRefresh = false;

        if (_disposed) return;

        var updates = PropertyUpdateQueue.DrainAll();
        foreach (var propertyName in updates)
        {
            OnPropertyChanged(propertyName);
        }
    }

    // ─── Disposal ─────────────────────────────────────────────────────────────

    /// <summary>Releases all managed resources.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override in subclass to release additional resources.
    /// Always call base.Dispose(disposing).
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            _throttleTimer.Stop();
            _throttleTimer.Tick -= OnThrottleTick;
            Subscriptions.Dispose();
            PropertyUpdateQueue.Clear();
        }
    }

    /// <summary>Finalizer fallback — logs if subclass forgot to call Dispose.</summary>
    ~ViewModelBase()
    {
        Dispose(disposing: false);
    }
}
