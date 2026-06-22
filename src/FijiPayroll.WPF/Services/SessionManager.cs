using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.WPF.Views.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Manages user session inactivity timeout.
/// Tracks mouse movement, keyboard activity, and navigation.
/// After 30 minutes of inactivity, logs out the user and prompts for re-authentication.
/// </summary>
public sealed class SessionManager : IDisposable
{
    private readonly IAuthSessionStore _sessionStore;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionManager> _logger;
    private DispatcherTimer? _timer;
    private readonly object _lock = new();
    private bool _isTracking;

    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMinutes(30);

    public SessionManager(
        IAuthSessionStore sessionStore,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ILogger<SessionManager> logger)
    {
        _sessionStore = sessionStore;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Starts tracking user inactivity.
    /// </summary>
    public void StartTracking()
    {
        lock (_lock)
        {
            if (_isTracking) return;
            _isTracking = true;
        }

        _logger.LogInformation("Initializing session inactivity tracker (30-minute timeout).");

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // Subscribe to global inputs
            InputManager.Current.PreProcessInput += OnInputActivity;

            // Subscribe to navigation events
            _navigationService.CurrentViewChanged += OnNavigationActivity;

            // Initialize timer
            _timer = new DispatcherTimer
            {
                Interval = TimeoutDuration
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        });
    }

    /// <summary>
    /// Stops tracking user inactivity.
    /// </summary>
    public void StopTracking()
    {
        lock (_lock)
        {
            if (!_isTracking) return;
            _isTracking = false;
        }

        _logger.LogInformation("Stopping session inactivity tracker.");

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            InputManager.Current.PreProcessInput -= OnInputActivity;
            _navigationService.CurrentViewChanged -= OnNavigationActivity;

            _timer?.Stop();
            _timer = null;
        });
    }

    private void ResetTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Start();
        }
    }

    private void OnInputActivity(object sender, PreProcessInputEventArgs e)
    {
        // Reset timer on any mouse movement, keyboard, or other input activity
        ResetTimer();
    }

    private void OnNavigationActivity()
    {
        _logger.LogDebug("Session reset: Navigation activity detected.");
        ResetTimer();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _logger.LogWarning("Inactivity timeout reached (30 minutes). Logging out user.");
        
        // Stop tracking temporarily to avoid loops
        StopTracking();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                // Clear session
                _sessionStore.Clear();

                // Get current main window
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Hide();
                }

                // Show Login screen
                var loginView = _serviceProvider.GetRequiredService<LoginView>();
                bool? loginSuccess = loginView.ShowDialog();

                if (loginSuccess == true)
                {
                    _logger.LogInformation("User re-authenticated successfully after timeout.");
                    if (mainWindow != null)
                    {
                        mainWindow.Show();
                    }
                    // Restart tracking
                    StartTracking();
                }
                else
                {
                    _logger.LogWarning("Re-authentication cancelled after timeout. Shutting down.");
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling session timeout logout.");
                System.Windows.Application.Current.Shutdown();
            }
        });
    }

    public void Dispose()
    {
        StopTracking();
    }
}
