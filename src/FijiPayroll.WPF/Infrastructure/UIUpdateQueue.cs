using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Policy applied when the UI update queue is at capacity.
/// </summary>
public enum QueueOverflowPolicy
{
    /// <summary>Remove the oldest pending update before enqueuing the new one.</summary>
    DropOldest,
    /// <summary>Discard the incoming update if queue is full.</summary>
    DropNewest,
    /// <summary>Replace the last pending item with the incoming update (merge/coalesce).</summary>
    Coalesce
}

/// <summary>
/// Bounded, thread-safe UI update queue for ViewModel property update batching.
/// Max size: 500 items. Configurable overflow policy.
/// </summary>
/// <typeparam name="T">Type of UI update payload.</typeparam>
public sealed class UIUpdateQueue<T>
{
    private readonly LinkedList<T> _queue = new();
    private readonly object _lock = new();
    private readonly int _maxCapacity;
    private readonly QueueOverflowPolicy _policy;
    private readonly Action<T>? _applyCallback;

    public UIUpdateQueue(
        int maxCapacity = 500,
        QueueOverflowPolicy policy = QueueOverflowPolicy.DropOldest,
        Action<T>? applyCallback = null)
    {
        _maxCapacity = maxCapacity;
        _policy = policy;
        _applyCallback = applyCallback;
    }

    /// <summary>Gets the current number of pending items in the queue.</summary>
    public int Count { get { lock (_lock) return _queue.Count; } }

    /// <summary>
    /// Enqueues an update, applying the configured overflow policy if at capacity.
    /// </summary>
    public void Enqueue(T item)
    {
        lock (_lock)
        {
            if (_queue.Count >= _maxCapacity)
            {
                switch (_policy)
                {
                    case QueueOverflowPolicy.DropOldest:
                        _queue.RemoveFirst();
                        break;
                    case QueueOverflowPolicy.DropNewest:
                        return; // Discard incoming
                    case QueueOverflowPolicy.Coalesce:
                        if (_queue.Last != null)
                            _queue.RemoveLast(); // Replace last with incoming
                        break;
                }
            }
            _queue.AddLast(item);
        }

        // If a callback is wired, dispatch immediately
        _applyCallback?.Invoke(item);
    }

    /// <summary>
    /// Drains all pending items and returns them as a snapshot list.
    /// </summary>
    public IReadOnlyList<T> DrainAll()
    {
        lock (_lock)
        {
            var result = new List<T>(_queue);
            _queue.Clear();
            return result;
        }
    }

    /// <summary>
    /// Attempts to dequeue the oldest pending item.
    /// </summary>
    public bool TryDequeue(out T? item)
    {
        lock (_lock)
        {
            if (_queue.Count == 0) { item = default; return false; }
            item = _queue.First!.Value;
            _queue.RemoveFirst();
            return true;
        }
    }

    /// <summary>
    /// Clears all pending updates.
    /// </summary>
    public void Clear() { lock (_lock) _queue.Clear(); }
}
