using System;
using System.Collections.Generic;
using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Immutable snapshot of navigation state taken before a navigation transaction begins.
/// Used to restore navigation stacks if the transaction is rolled back.
/// </summary>
public sealed class NavigationSnapshot
{
    public Type? CurrentViewType { get; }
    public IReadOnlyList<Type> BackStack { get; }
    public IReadOnlyList<Type> ForwardStack { get; }
    public DateTime TakenAt { get; }

    public NavigationSnapshot(Type? currentViewType, IReadOnlyList<Type> backStack, IReadOnlyList<Type> forwardStack)
    {
        CurrentViewType = currentViewType;
        BackStack = new List<Type>(backStack).AsReadOnly();
        ForwardStack = new List<Type>(forwardStack).AsReadOnly();
        TakenAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a navigation transaction scope. Provides commit/rollback semantics
/// around a navigation operation. Acquired from NavigationService via BeginTransaction().
/// </summary>
public sealed class NavigationTransactionScope : IDisposable
{
    private readonly Action<NavigationSnapshot> _rollbackAction;
    private readonly NavigationSnapshot _snapshot;
    private bool _committed;
    private bool _disposed;

    public NavigationTransactionScope(NavigationSnapshot snapshot, Action<NavigationSnapshot> rollbackAction)
    {
        _snapshot = snapshot;
        _rollbackAction = rollbackAction;
    }

    /// <summary>
    /// Marks the transaction as committed — no rollback will occur on Dispose.
    /// </summary>
    public void Commit()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(NavigationTransactionScope));
        _committed = true;
    }

    /// <summary>
    /// Explicitly rolls back navigation state to the snapshot taken at transaction start.
    /// </summary>
    public void Rollback()
    {
        if (_disposed) return;
        _committed = false;
        _rollbackAction(_snapshot);
    }

    /// <summary>
    /// On dispose — if not committed, automatically rolls back.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_committed)
        {
            _rollbackAction(_snapshot);
        }
    }
}
