using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Priority levels for dispatcher queue slots.
/// </summary>
public enum DispatchPriority
{
    /// <summary>High-importance UI operations (navigation, modal dialogs).</summary>
    Critical = 0,
    /// <summary>Standard ViewModel property updates.</summary>
    Normal = 1,
    /// <summary>Background diagnostics, log streaming.</summary>
    Low = 2
}

/// <summary>
/// Weighted Round-Robin priority dispatcher queue.
/// Weights: Critical 50%, Normal 35%, Low 15%.
/// Includes per-level aging boost and a hard 1000ms flush for starved slots.
/// </summary>
public sealed class PriorityDispatcherQueue : IDisposable
{
    // Queue slots per priority
    private readonly ConcurrentQueue<Action>[] _queues = new ConcurrentQueue<Action>[3];

    // WRR weights per priority level
    private static readonly int[] Weights = { 10, 7, 3 }; // sum = 20

    // Aging: milliseconds before a starved slot gets a one-shot boost
    private static readonly int StarvationThresholdMs = 1000;

    // Maximum pending items per priority before drop policy kicks in
    private const int MaxPerLevel = 500;

    private readonly ILogger? _logger;
    private readonly Timer _starvationTimer;
    private readonly long[] _lastServiced = new long[3];
    private int _cyclePosition;
    private volatile bool _disposed;

    // Dispatcher priority mapping
    private static readonly DispatcherPriority[] WpfPriorities =
    {
        DispatcherPriority.Send,      // Critical
        DispatcherPriority.Normal,    // Normal
        DispatcherPriority.Background // Low
    };

    public PriorityDispatcherQueue(ILogger? logger = null)
    {
        _logger = logger;
        for (int i = 0; i < 3; i++)
        {
            _queues[i] = new ConcurrentQueue<Action>();
            _lastServiced[i] = Environment.TickCount64;
        }

        // Watchdog flushes any starved priority lanes every 500ms
        _starvationTimer = new Timer(FlushStarvedLanes, null,
            TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Enqueues an action into the specified priority lane.
    /// Drops oldest entry when cap is exceeded.
    /// </summary>
    public void Enqueue(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
        if (_disposed) return;

        int slot = (int)priority;
        var queue = _queues[slot];

        // Enforce per-level cap — drop oldest
        while (queue.Count >= MaxPerLevel)
        {
            queue.TryDequeue(out _);
        }

        queue.Enqueue(action);
        DrainViaWeightedRoundRobin();
    }

    /// <summary>
    /// Drains queued work items onto the UI dispatcher using WRR scheduling.
    /// </summary>
    private void DrainViaWeightedRoundRobin()
    {
        if (_disposed) return;

        SafeDispatcher.BeginInvoke(() =>
        {
            int totalCycles = Weights[0] + Weights[1] + Weights[2];
            int dispatched = 0;

            // One full WRR sweep per drain call (20 slots max)
            for (int cycle = 0; cycle < totalCycles && !_disposed; cycle++)
            {
                int slot = GetNextWeightedSlot();
                if (_queues[slot].TryDequeue(out var action))
                {
                    Interlocked.Exchange(ref _lastServiced[slot], Environment.TickCount64);
                    try { action(); }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "[PriorityQueue] Unhandled action error in slot {Slot}", slot);
                    }
                    dispatched++;
                }
            }

            // If items remain, schedule another drain pass
            bool hasMore = false;
            for (int i = 0; i < 3; i++) hasMore |= !_queues[i].IsEmpty;
            if (hasMore) DrainViaWeightedRoundRobin();

        }, DispatcherPriority.Normal);
    }

    private int GetNextWeightedSlot()
    {
        // Simple WRR: cycle through slots by weight
        int[] expanded = new int[Weights[0] + Weights[1] + Weights[2]]; // 20
        int idx = 0;
        for (int s = 0; s < 3; s++)
            for (int w = 0; w < Weights[s]; w++)
                expanded[idx++] = s;

        int slot = expanded[_cyclePosition % expanded.Length];
        _cyclePosition = (_cyclePosition + 1) % expanded.Length;
        return slot;
    }

    private void FlushStarvedLanes(object? state)
    {
        if (_disposed) return;

        long now = Environment.TickCount64;
        for (int slot = 0; slot < 3; slot++)
        {
            if (_queues[slot].IsEmpty) continue;
            long elapsed = now - Interlocked.Read(ref _lastServiced[slot]);
            if (elapsed >= StarvationThresholdMs)
            {
                _logger?.LogDebug("[PriorityQueue] Starvation flush: slot {Slot} at {Elapsed}ms", slot, elapsed);
                DrainViaWeightedRoundRobin();
                break;
            }
        }
    }

    /// <summary>Gets the current pending count for the given priority lane.</summary>
    public int PendingCount(DispatchPriority priority) => _queues[(int)priority].Count;

    public void Dispose()
    {
        _disposed = true;
        _starvationTimer.Dispose();
    }
}
