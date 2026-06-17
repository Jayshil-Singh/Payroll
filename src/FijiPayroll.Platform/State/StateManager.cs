using System.Collections.Concurrent;

namespace FijiPayroll.Platform.State;

/// <summary>
/// Controls system-wide state transactions, managing locked periods, double-payment guards, and system maintenance modes.
/// </summary>
public sealed class StateManager
{
    private readonly ConcurrentDictionary<int, bool> _lockedPeriods = new();
    private readonly ConcurrentDictionary<string, bool> _activeTransactions = new();
    private bool _isMaintenanceMode;

    /// <summary>
    /// Gets or sets a value indicating whether the system is in maintenance mode.
    /// </summary>
    public bool IsMaintenanceMode
    {
        get => _isMaintenanceMode;
        set => _isMaintenanceMode = value;
    }

    /// <summary>
    /// Checks if a pay period is locked for changes.
    /// </summary>
    public bool IsPeriodLocked(int periodId)
    {
        return _lockedPeriods.TryGetValue(periodId, out bool locked) && locked;
    }

    /// <summary>
    /// Locks a pay period.
    /// </summary>
    public void LockPeriod(int periodId)
    {
        _lockedPeriods[periodId] = true;
    }

    /// <summary>
    /// Unlocks a pay period.
    /// </summary>
    public void UnlockPeriod(int periodId)
    {
        _lockedPeriods[periodId] = false;
    }

    /// <summary>
    /// Attempts to acquire an exclusive lock on an operation to prevent double execution (e.g. processing same bank file twice).
    /// </summary>
    /// <param name="operationKey">The unique operation signature key.</param>
    /// <returns>True if the lock was successfully acquired; false if it's already active.</returns>
    public bool TryAcquireOperationLock(string operationKey)
    {
        return _activeTransactions.TryAdd(operationKey, true);
    }

    /// <summary>
    /// Releases an exclusive lock on an operation.
    /// </summary>
    /// <param name="operationKey">The unique operation signature key.</param>
    public void ReleaseOperationLock(string operationKey)
    {
        _activeTransactions.TryRemove(operationKey, out _);
    }
}
