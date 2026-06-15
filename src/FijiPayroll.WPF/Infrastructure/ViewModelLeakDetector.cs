using System;
using System.Runtime.CompilerServices;
using System.Threading;
using FijiPayroll.WPF.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Tracks ViewModel instances via weak references and counts how many are still alive
/// (i.e., not yet GC-collected). Used in diagnostics HUD to detect memory leaks.
/// Only active in DEBUG builds to avoid overhead in production.
/// </summary>
public sealed class ViewModelLeakDetector
{
    private static readonly ConditionalWeakTable<ViewModelBase, LeakRecord> _table = new();
    private static int _registeredCount;
    private static int _finalizedCount;

    private readonly ILogger? _logger;

    public ViewModelLeakDetector(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Registers a ViewModel for leak tracking.</summary>
    public void Register(ViewModelBase vm)
    {
#if DEBUG
        Interlocked.Increment(ref _registeredCount);
        var record = new LeakRecord(vm.GetType().Name, _logger);
        _table.Add(vm, record);
#endif
    }

    /// <summary>
    /// Static registration entry-point — called from ViewModelBase constructor
    /// without requiring a resolver instance.
    /// </summary>
    public static void StaticRegister(ViewModelBase vm)
    {
#if DEBUG
        Interlocked.Increment(ref _registeredCount);
        var record = new LeakRecord(vm.GetType().Name, null);
        _table.Add(vm, record);
#endif
    }

    /// <summary>Gets the total count of ViewModels ever registered since startup.</summary>
    public static int RegisteredCount => _registeredCount;

    /// <summary>
    /// Gets an approximate count of ViewModels still in memory (not GC'd).
    /// NOTE: Due to GC non-determinism this is approximate.
    /// </summary>
    public static int EstimatedLiveCount => _registeredCount - _finalizedCount;

    /// <summary>Records a ViewModel finalization (triggered by GC).</summary>
    internal static void OnFinalized() => Interlocked.Increment(ref _finalizedCount);

    /// <summary>Resets counters — use only in test teardown scenarios.</summary>
    internal static void Reset()
    {
        Interlocked.Exchange(ref _registeredCount, 0);
        Interlocked.Exchange(ref _finalizedCount, 0);
    }

    // Attached per-ViewModel record; finalizer is called when VM is GC'd
    private sealed class LeakRecord
    {
        private readonly string _vmTypeName;
        private readonly ILogger? _logger;

        public LeakRecord(string vmTypeName, ILogger? logger)
        {
            _vmTypeName = vmTypeName;
            _logger = logger;
        }

        ~LeakRecord()
        {
            ViewModelLeakDetector.OnFinalized();
            _logger?.LogDebug("[LeakDetector] ViewModel finalized: {Type}", _vmTypeName);
        }
    }
}
