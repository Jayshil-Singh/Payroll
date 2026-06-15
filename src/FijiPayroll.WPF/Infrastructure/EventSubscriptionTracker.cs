using System;
using System.Collections.Generic;
using System.Threading;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Tracks event subscriptions for a ViewModel instance and detaches all handlers
/// deterministically when the ViewModel is disposed, preventing event-handler memory leaks.
/// </summary>
public sealed class EventSubscriptionTracker : IDisposable
{
    private readonly List<Action> _detachActions = new();
    private readonly object _lock = new();
    private volatile bool _disposed;

    /// <summary>
    /// Registers an event subscription with its corresponding detach action.
    /// </summary>
    /// <param name="attach">Action that adds the handler to the event source.</param>
    /// <param name="detach">Action that removes the handler from the event source.</param>
    public void Track(Action attach, Action detach)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventSubscriptionTracker),
                "Cannot track subscriptions on a disposed tracker.");
        }

        lock (_lock)
        {
            _detachActions.Add(detach);
        }

        attach();
    }

    /// <summary>
    /// Registers a typed event subscription using EventHandler&lt;T&gt; pattern.
    /// </summary>
    public void Track<TArgs>(
        Action<EventHandler<TArgs>> subscribe,
        Action<EventHandler<TArgs>> unsubscribe,
        EventHandler<TArgs> handler)
    {
        Track(() => subscribe(handler), () => unsubscribe(handler));
    }

    /// <summary>
    /// Registers a typed event subscription using Action delegate pattern (e.g., Action&lt;LogEntry&gt;).
    /// </summary>
    public void Track<T>(
        Action<Action<T>> subscribe,
        Action<Action<T>> unsubscribe,
        Action<T> handler)
    {
        Track(() => subscribe(handler), () => unsubscribe(handler));
    }

    /// <summary>
    /// Registers a parameterless event (Action) subscription.
    /// </summary>
    public void TrackAction(
        Action<Action> subscribe,
        Action<Action> unsubscribe,
        Action handler)
    {
        Track(() => subscribe(handler), () => unsubscribe(handler));
    }

    /// <summary>
    /// Detaches all tracked subscriptions and marks the tracker as disposed.
    /// Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        List<Action> snapshot;
        lock (_lock)
        {
            snapshot = new List<Action>(_detachActions);
            _detachActions.Clear();
        }

        foreach (var detach in snapshot)
        {
            try { detach(); }
            catch { /* Swallow — source may already be GC'd */ }
        }
    }

    /// <summary>Gets the number of currently tracked subscriptions.</summary>
    public int TrackedCount { get { lock (_lock) return _detachActions.Count; } }
}
