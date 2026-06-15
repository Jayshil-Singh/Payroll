using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FijiPayroll.WPF.Infrastructure;

/// <summary>
/// Thread-safe dispatcher wrapper that prevents re-entrant deadlocks and ensures
/// all UI operations execute correctly regardless of calling thread context.
/// </summary>
public static class SafeDispatcher
{
    // Guard against deeply re-entrant calls on UI thread
    [ThreadStatic]
    private static int _dispatchDepth;

    private const int MaxReentrantDepth = 8;

    /// <summary>
    /// Gets the WPF application dispatcher, falling back safely if not yet initialized.
    /// </summary>
    private static Dispatcher? AppDispatcher =>
        System.Windows.Application.Current?.Dispatcher;

    /// <summary>
    /// Returns true if the current thread is the UI dispatcher thread.
    /// </summary>
    public static bool IsOnUIThread =>
        AppDispatcher?.CheckAccess() ?? Thread.CurrentThread.IsBackground == false;

    /// <summary>
    /// Invokes <paramref name="action"/> on the UI thread synchronously.
    /// If already on the UI thread, executes inline (safe from deadlock).
    /// </summary>
    public static void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        if (AppDispatcher == null) { action(); return; }

        if (AppDispatcher.CheckAccess())
        {
            if (_dispatchDepth >= MaxReentrantDepth)
            {
                // Defer to avoid deeply-nested re-entrant call stacks
                AppDispatcher.BeginInvoke(priority, action);
                return;
            }

            _dispatchDepth++;
            try { action(); }
            finally { _dispatchDepth--; }
        }
        else
        {
            AppDispatcher.Invoke(priority, action);
        }
    }

    /// <summary>
    /// Posts <paramref name="action"/> to the UI thread asynchronously (fire-and-forget safe).
    /// </summary>
    public static void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        AppDispatcher?.BeginInvoke(priority, action);
    }

    /// <summary>
    /// Awaitable version — posts <paramref name="action"/> to UI thread and returns a Task.
    /// </summary>
    public static Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        if (AppDispatcher == null) { action(); return Task.CompletedTask; }

        if (AppDispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return AppDispatcher.InvokeAsync(action, priority, cancellationToken).Task;
    }

    /// <summary>
    /// Awaitable version — posts <paramref name="func"/> to UI thread and returns Task&lt;T&gt;.
    /// </summary>
    public static Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        if (AppDispatcher == null) return Task.FromResult(func());

        if (AppDispatcher.CheckAccess())
            return Task.FromResult(func());

        return AppDispatcher.InvokeAsync(func, priority, cancellationToken).Task;
    }

    /// <summary>
    /// Runs <paramref name="action"/> on UI thread ensuring it will not throw if app is shutting down.
    /// </summary>
    public static void SafeBeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        try
        {
            var dispatcher = AppDispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            dispatcher.BeginInvoke(priority, action);
        }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
    }
}
