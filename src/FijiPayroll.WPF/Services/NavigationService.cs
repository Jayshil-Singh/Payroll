using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.ViewModels;
using FijiPayroll.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Thread-safe, transactional navigation service.
/// - All navigation requests are serialized through a SemaphoreSlim lock.
/// - Consecutive duplicate navigations are suppressed.
/// - Navigation state is persisted asynchronously.
/// - Outgoing ViewModels are disposed on transition if they implement IDisposable.
/// - Full commit/rollback transaction semantics via NavigationTransactionScope.
/// </summary>
public sealed class NavigationService : INavigationService, IDisposable
{
    private const int MaxHistorySize = 50;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly string _stateFilePath;

    private readonly List<Type> _backStack = new();
    private readonly List<Type> _forwardStack = new();
    private ViewModelBase? _currentView;
    private NavigationScopeHandle? _currentScopeHandle;
    private volatile bool _disposed;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nav_state.json");
    }

    // ─── INavigationService ──────────────────────────────────────────────────

    /// <inheritdoc />
    public ViewModelBase? CurrentView => _currentView;

    /// <inheritdoc />
    public bool CanGoBack => _backStack.Count > 0;

    /// <inheritdoc />
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <inheritdoc />
    public string DerivedBreadcrumbPath
    {
        get
        {
            if (_currentView == null) return "Home";
            return _currentView.GetType().Name switch
            {
                nameof(DashboardViewModel)  => "Home > Dashboard",
                nameof(EmployeeViewModel)   => "Home > Employees",
                nameof(PayrollViewModel)    => "Home > Payroll Cycles",
                nameof(SetupViewModel)      => "Home > Component Setup",
                nameof(ReportsViewModel)    => "Home > Statutory Reports",
                nameof(AdminViewModel)      => "Home > Administration",
                nameof(LogViewerViewModel)  => "Home > Administration > Log Viewer",
                nameof(CompanySetupDashboardViewModel) => "Home > Onboarding Wizard",
                var n                       => $"Home > {n.Replace("ViewModel", "")}"
            };
        }
    }

    /// <inheritdoc />
    public event Action? CurrentViewChanged;

    // ─── Navigation (serialized) ──────────────────────────────────────────────

    /// <inheritdoc />
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        => NavigateTo(typeof(TViewModel));

    /// <inheritdoc />
    public void NavigateTo(Type viewModelType, bool clearForwardHistory = true)
    {
        // Fire-and-forget into async serialized pipeline
        _ = NavigateAsync(viewModelType, clearForwardHistory);
    }

    private async Task NavigateAsync(Type viewModelType, bool clearForwardHistory)
    {
        if (_disposed) return;

        // Thread safety: Serialize all navigation requests using SemaphoreSlim lock
        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning("[Navigation] Lock timeout — navigation to {VM} dropped.", viewModelType.Name);
            return;
        }

        try
        {
            // Suppress consecutive duplicate navigation
            if (_currentView != null && _currentView.GetType() == viewModelType)
                return;

            // Phase 1: Prepare navigation (Create scope, resolve next VM, validate)
            var snapshot = TakeSnapshot();
            var nextScopeHandle = new NavigationScopeHandle(_serviceProvider.CreateScope());
            ViewModelBase nextVm;
            try
            {
                nextVm = (ViewModelBase)nextScopeHandle.ServiceProvider.GetRequiredService(viewModelType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Navigation] Phase 1 preparation failed: failed to resolve {VM}", viewModelType.Name);
                nextScopeHandle.Dispose();
                return;
            }

            // Phase 2: Commit navigation (Atomic stack updates and UI transitions)
            var outgoingVm = _currentView;
            var outgoingScopeHandle = _currentScopeHandle;

            using var txn = new NavigationTransactionScope(snapshot, ApplySnapshot);
            try
            {
                // Atomic stack update inside lock
                if (outgoingVm != null)
                {
                    _backStack.Add(outgoingVm.GetType());
                    TrimStack(_backStack);
                }
                if (clearForwardHistory) _forwardStack.Clear();

                // Commit transaction state
                txn.Commit();

                // Switch view on UI thread with confirmation callback
                await SafeDispatcher.InvokeAsync(() =>
                {
                    _currentView = nextVm;
                    _currentScopeHandle = nextScopeHandle;
                    CurrentViewChanged?.Invoke();
                });

                // UI transition has successfully completed. Safe to persist state.
                _ = SaveStateAsync(viewModelType.Name);

                _logger.LogDebug("[Navigation] Navigated to {VM}", viewModelType.Name);

                // Dispose of the old scope only after UI confirmation that the switch is done
                outgoingScopeHandle?.Release();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Navigation] Phase 2 commit failed for {VM} — rolling back.", viewModelType.Name);
                nextScopeHandle.Dispose();
                // txn.Dispose() will auto-rollback since Commit was not called
            }
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (!CanGoBack || _currentView == null) return;
        _ = GoBackAsync();
    }

    private async Task GoBackAsync()
    {
        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5))) return;
        try
        {
            if (_backStack.Count == 0 || _currentView == null) return;

            var snapshot = TakeSnapshot();
            using var txn = new NavigationTransactionScope(snapshot, ApplySnapshot);
            try
            {
                Type previousType = _backStack[^1];
                _backStack.RemoveAt(_backStack.Count - 1);

                _forwardStack.Add(_currentView.GetType());
                TrimStack(_forwardStack);

                txn.Commit();
            }
            catch
            {
                return; // auto-rollback
            }
        }
        finally { _navigationLock.Release(); }

        // Navigate outside the lock using the safe two-phase system
        await NavigateInternalAsync(_backStack.Count > 0
            ? _backStack[^1]
            : typeof(DashboardViewModel), clearForwardHistory: false);
    }

    /// <inheritdoc />
    public void GoForward()
    {
        if (!CanGoForward || _currentView == null) return;
        _ = GoForwardAsync();
    }

    private async Task GoForwardAsync()
    {
        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5))) return;
        Type? nextType;
        try
        {
            if (_forwardStack.Count == 0) return;
            nextType = _forwardStack[^1];
            _forwardStack.RemoveAt(_forwardStack.Count - 1);
        }
        finally { _navigationLock.Release(); }

        await NavigateInternalAsync(nextType, clearForwardHistory: false);
    }

    private async Task NavigateInternalAsync(Type vmType, bool clearForwardHistory)
    {
        // Thread safety: Serialize all navigation requests using SemaphoreSlim lock
        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5))) return;
        try
        {
            if (_currentView?.GetType() == vmType) return;

            // Phase 1: Prepare
            var nextScopeHandle = new NavigationScopeHandle(_serviceProvider.CreateScope());
            ViewModelBase nextVm;
            try
            {
                nextVm = (ViewModelBase)nextScopeHandle.ServiceProvider.GetRequiredService(vmType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Navigation] NavigateInternalAsync Phase 1 failed for {VM}", vmType.Name);
                nextScopeHandle.Dispose();
                return;
            }

            // Phase 2: Commit
            var outgoing = _currentView;
            var outgoingScopeHandle = _currentScopeHandle;

            await SafeDispatcher.InvokeAsync(() =>
            {
                _currentView = nextVm;
                _currentScopeHandle = nextScopeHandle;
                CurrentViewChanged?.Invoke();
            });

            _ = SaveStateAsync(vmType.Name);

            // Safe disposal of old scope handle after UI confirmation
            outgoingScopeHandle?.Release();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Navigation] NavigateInternalAsync Phase 2 failed for {VM}", vmType.Name);
        }
        finally { _navigationLock.Release(); }
    }

    /// <inheritdoc />
    public void RestoreLastState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                string json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<NavigationState>(json);
                if (state != null && !string.IsNullOrWhiteSpace(state.LastActiveViewModel))
                {
                    var type = typeof(DashboardViewModel).Assembly
                        .GetTypes()
                        .FirstOrDefault(t =>
                            t.Name == state.LastActiveViewModel &&
                            typeof(ViewModelBase).IsAssignableFrom(t));

                    if (type != null)
                    {
                        NavigateTo(type);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Navigation] Could not restore last nav state — defaulting to Dashboard.");
        }

        NavigateTo<DashboardViewModel>();
    }

    // ─── Transaction Helpers ─────────────────────────────────────────────────

    private NavigationSnapshot TakeSnapshot()
        => new(_currentView?.GetType(),
               new List<Type>(_backStack),
               new List<Type>(_forwardStack));

    private void ApplySnapshot(NavigationSnapshot snapshot)
    {
        _backStack.Clear();
        _backStack.AddRange(snapshot.BackStack);

        _forwardStack.Clear();
        _forwardStack.AddRange(snapshot.ForwardStack);

        // Restore view if it changed
        if (snapshot.CurrentViewType != null && _currentView?.GetType() != snapshot.CurrentViewType)
        {
            try
            {
                var nextScopeHandle = new NavigationScopeHandle(_serviceProvider.CreateScope());
                var vm = (ViewModelBase)nextScopeHandle.ServiceProvider.GetRequiredService(snapshot.CurrentViewType);
                SafeDispatcher.SafeBeginInvoke(() =>
                {
                    var outgoingScopeHandle = _currentScopeHandle;
                    _currentView = vm;
                    _currentScopeHandle = nextScopeHandle;
                    CurrentViewChanged?.Invoke();

                    // Release the old scope handle only after UI confirmation
                    outgoingScopeHandle?.Release();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Navigation] Snapshot restore failed for {VM}", snapshot.CurrentViewType.Name);
            }
        }
    }

    private static void TrimStack(List<Type> stack)
    {
        while (stack.Count > MaxHistorySize)
            stack.RemoveAt(0);
    }

    // ─── Persistence ─────────────────────────────────────────────────────────

    private Task SaveStateAsync(string viewModelName)
        => Task.Run(() =>
        {
            try
            {
                var state = new NavigationState { LastActiveViewModel = viewModelName };
                string json = JsonSerializer.Serialize(state);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Navigation] Failed to persist nav state.");
            }
        });

    private class NavigationState
    {
        public string LastActiveViewModel { get; set; } = string.Empty;
    }

    // ─── Dispose ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _navigationLock.Dispose();
        _currentScopeHandle?.Release();
    }
}

/// <summary>
/// Reference-counted wrapper around IServiceScope to manage UI ViewModel lifetimes safely.
/// Thread safety: All internal count modifications are synchronized using lock.
/// </summary>
public sealed class NavigationScopeHandle : IDisposable
{
    private readonly IServiceScope _scope;
    private int _refCount = 1;
    private bool _disposed;
    private readonly object _lock = new();

    public NavigationScopeHandle(IServiceScope scope)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public IServiceProvider ServiceProvider => _scope.ServiceProvider;

    public void AddRef()
    {
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NavigationScopeHandle));
            _refCount++;
        }
    }

    public void Release()
    {
        bool shouldDispose = false;
        lock (_lock)
        {
            if (_disposed) return;
            _refCount--;
            if (_refCount <= 0)
            {
                _disposed = true;
                shouldDispose = true;
            }
        }

        if (shouldDispose)
        {
            _scope.Dispose();
        }
    }

    public void Dispose()
    {
        Release();
    }
}
