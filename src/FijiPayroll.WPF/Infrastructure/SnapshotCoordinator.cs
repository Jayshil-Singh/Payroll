using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Global system-wide snapshot coordinator.
/// Provides a single exclusive barrier lock that freezes all subsystems
/// (navigation, state store, log buffer) before taking a state snapshot,
/// ensuring consistency under heavy parallel load.
/// 
/// Usage pattern:
///   await using var freeze = await SnapshotCoordinator.EnterFreezeAsync();
///   // Take snapshots of all subsystems here
///   // Freeze releases automatically on dispose
/// </summary>
public sealed class SnapshotCoordinator : IDisposable
{
    private static readonly SemaphoreSlim _globalBarrier = new(1, 1);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger? _logger;
    private volatile bool _disposed;

    private SnapshotCoordinator(ILogger? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Acquires the global freeze barrier exclusively.
    /// All subsystems must check <see cref="IsFrozen"/> before mutating shared state.
    /// Times out after 10 seconds to prevent deadlocks.
    /// </summary>
    public static async Task<SnapshotCoordinator> EnterFreezeAsync(
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        bool acquired = await _globalBarrier.WaitAsync(DefaultTimeout, cancellationToken)
            .ConfigureAwait(false);

        if (!acquired)
        {
            logger?.LogWarning("[SnapshotCoordinator] Freeze barrier timeout — proceeding without full freeze.");
        }

        var coordinator = new SnapshotCoordinator(logger);
        coordinator._barrierAcquired = acquired;
        IsFrozen = true;
        return coordinator;
    }

    // ─── Global freeze signal ─────────────────────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether the system is currently in a snapshot freeze.
    /// Subsystems should poll this flag and defer mutations during freeze.
    /// </summary>
    public static volatile bool IsFrozen;

    private bool _barrierAcquired;

    /// <summary>Releases the freeze barrier and resumes normal subsystem operation.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        IsFrozen = false;

        if (_barrierAcquired)
        {
            _globalBarrier.Release();
            _logger?.LogDebug("[SnapshotCoordinator] Freeze barrier released.");
        }
    }
}
