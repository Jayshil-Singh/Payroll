using FijiPayroll.Application;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Persistence;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Seeders;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Infrastructure;
using FijiPayroll.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF;

/// <summary>
/// Application bootstrapper with full enterprise hardening:
/// - DI container initialization
/// - Database migration + seeding
/// - State recovery (persisted + validated)
/// - Monitor/watchdog startup
/// - 5-second graceful shutdown with forced termination fallback
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;
    private string _currentTheme = "Dark";
    private readonly string _themeConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme_config.txt");

    // Monitor references for orderly shutdown
    private SystemHealthMonitor? _healthMonitor;
    private SystemIntegrityValidator? _integrityValidator;
    private MemorySmoothingScheduler? _memoryScheduler;
    private PriorityDispatcherQueue? _priorityQueue;
    private ComplianceJobProcessor? _complianceJobProcessor;

    public IServiceProvider? ServiceProvider => _serviceProvider;
    public string CurrentTheme => _currentTheme;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Initialize and show splash screen
        var splash = new SplashWindow();
        splash.Show();

        Task.Run(async () =>
        {
            try
            {
                // ── Stage 1: DI Container ───────────────────────────────────────────
                splash.UpdateProgress(20, "Bootstrapping Container", "Loading services and features...");
                await Task.Delay(300);

                var services = new ServiceCollection();

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string dbDir = Path.Combine(userProfile, "FijiPayroll");
                if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

                string connectionString = $"Server=(localdb)\\mssqllocaldb;Database=FijiPayrollDb;Trusted_Connection=True;" +
                    $"MultipleActiveResultSets=true;AttachDbFileName={Path.Combine(dbDir, "FijiPayrollDb.mdf")}";

                // Shared log buffer — must be registered before DI build
                var logBuffer = new LogBuffer();
                services.AddSingleton<ILogBuffer>(logBuffer);
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new InMemoryLoggerProvider(logBuffer));
                });

                // Infrastructure monitors (singletons, registered before AddPresentation)
                _priorityQueue = new PriorityDispatcherQueue();
                services.AddSingleton(_priorityQueue);
                services.AddSingleton<SystemHealthMonitor>();
                services.AddSingleton<SystemIntegrityValidator>();
                services.AddSingleton<MemorySmoothingScheduler>();
                services.AddSingleton<ViewModelLeakDetector>();

                // Application layers
                services.AddApplication();
                services.AddPersistence(connectionString);
                var configuration = new ConfigurationBuilder().Build();
                services.AddInfrastructure(configuration);

                // Security
                services.AddSingleton<WpfCurrentUserService>();
                services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<WpfCurrentUserService>());
                services.AddSingleton<ICurrentUserAccessor>(sp => sp.GetRequiredService<WpfCurrentUserService>());

                // WPF presentation layer (ViewModels, Views, Shell services)
                services.AddPresentation();

                _serviceProvider = services.BuildServiceProvider();
                _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                _logger.LogInformation("DI Container successfully initialized.");

                // ── Stage 2: Database ────────────────────────────────────────────────
                splash.UpdateProgress(40, "Connecting Database", "Opening local SQL Server database...");
                await Task.Delay(300);

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();
                
                // Initialize database tables for dynamic plugins
                var pluginLoader = _serviceProvider.GetRequiredService<PluginLoader>();
                await pluginLoader.InitializePluginsDatabaseAsync(scope.ServiceProvider);

                _logger.LogInformation("Database connected, schema validated, and plugin schemas registered.");

                // ── Stage 3: Seeders ─────────────────────────────────────────────────
                splash.UpdateProgress(60, "Running Seeders", "Loading Fiji tax tables & company components...");
                await Task.Delay(300);

                var ruleModuleSeeder = scope.ServiceProvider.GetRequiredService<RuleModuleSeeder>();
                await ruleModuleSeeder.SeedAsync();

                var taxSeeder = scope.ServiceProvider.GetRequiredService<TaxBracketSeeder>();
                await taxSeeder.SeedAsync();

                var componentSeeder = scope.ServiceProvider.GetRequiredService<PayrollComponentSeeder>();
                await componentSeeder.SeedAsync();

                var employeeSeeder = scope.ServiceProvider.GetRequiredService<EmployeeSeeder>();
                await employeeSeeder.SeedAsync();

                var complianceSeeder = scope.ServiceProvider.GetRequiredService<ComplianceSeeder>();
                await complianceSeeder.SeedAsync();
                _logger.LogInformation("FRCS tax tables, employee structures, and compliance rules/layouts seeded successfully.");

                // ── Stage 4: State Recovery ──────────────────────────────────────────
                splash.UpdateProgress(75, "Recovering State", "Restoring last session...");
                await Task.Delay(200);

                var stateStore = _serviceProvider.GetRequiredService<IApplicationStateStore>();
                await stateStore.LoadPersistedStateAsync();
                _logger.LogInformation("Application state restored.");

                // ── Stage 5: Start Monitors ──────────────────────────────────────────
                splash.UpdateProgress(90, "Starting Services", "Launching watchdog monitors...");

                _healthMonitor = _serviceProvider.GetRequiredService<SystemHealthMonitor>();
                _healthMonitor.Start();

                _integrityValidator = _serviceProvider.GetRequiredService<SystemIntegrityValidator>();
                _integrityValidator.Start();

                _memoryScheduler = _serviceProvider.GetRequiredService<MemorySmoothingScheduler>();
                _memoryScheduler.Start();

                _complianceJobProcessor = _serviceProvider.GetRequiredService<ComplianceJobProcessor>();
                _complianceJobProcessor.Start();

                _logger.LogInformation("All background monitors started.");

                // ── Stage 6: Open Shell ──────────────────────────────────────────────
                splash.UpdateProgress(100, "Opening Shell", "Launching main dashboard...");
                await Task.Delay(200);

                Dispatcher.Invoke(() =>
                {
                    LoadThemeSettings();

                    var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
                    navigationService.RestoreLastState();

                    var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                    mainWindow.Show();
                    splash.Close();
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to complete application boot sequence.");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Startup Error: {ex.Message}\n\n{ex.StackTrace}",
                        "Fatal Initialization Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    splash.Close();
                    Shutdown(-1);
                });
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application shutdown initiated.");

        // Graceful shutdown: give monitors up to 5 seconds to stop cleanly
        var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            Task.WhenAll(
                Task.Run(() => _healthMonitor?.Dispose()),
                Task.Run(() => _integrityValidator?.Dispose()),
                Task.Run(() => _memoryScheduler?.Dispose()),
                Task.Run(() => _priorityQueue?.Dispose()),
                Task.Run(() => _complianceJobProcessor?.Dispose())
            ).Wait(shutdownCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Graceful shutdown timed out — forcing termination.");
        }
        finally
        {
            shutdownCts.Dispose();
        }

        // Persist final application state
        if (_serviceProvider != null)
        {
            try
            {
                var stateStore = _serviceProvider.GetRequiredService<IApplicationStateStore>();
                stateStore.PersistCurrentStateAsync().Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
        }

        _logger?.LogInformation("Application exited cleanly.");
        base.OnExit(e);
    }

    // ─── Theme ────────────────────────────────────────────────────────────────

    public void LoadThemeSettings()
    {
        try
        {
            if (File.Exists(_themeConfigPath))
            {
                string theme = File.ReadAllText(_themeConfigPath).Trim();
                if (theme == "Dark" || theme == "Light")
                    SetTheme(theme);
            }
        }
        catch { }
    }

    public void SetTheme(string themeName)
    {
        var mergedDicts = Resources.MergedDictionaries;
        var themeDict = mergedDicts.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("Theme.Dark.xaml") ||
             d.Source.OriginalString.Contains("Theme.Light.xaml")));

        if (themeDict != null) mergedDicts.Remove(themeDict);

        string sourcePath = themeName == "Light" ? "Styles/Theme.Light.xaml" : "Styles/Theme.Dark.xaml";
        mergedDicts.Insert(0, new ResourceDictionary { Source = new Uri(sourcePath, UriKind.RelativeOrAbsolute) });
        _currentTheme = themeName;

        try { File.WriteAllText(_themeConfigPath, themeName); }
        catch { }
    }

    public void ToggleTheme() => SetTheme(_currentTheme == "Dark" ? "Light" : "Dark");

    // ─── Exception Handlers ───────────────────────────────────────────────────

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        HandleException(e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            HandleException(ex);
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        HandleException(e.Exception);
    }

    private void HandleException(Exception exception)
    {
        _logger?.LogError(exception, "Fatal unhandled exception occurred in application shell.");

        Dispatcher.Invoke(() =>
        {
            try
            {
                var errorWindow = new ErrorRecoveryWindow(exception);
                errorWindow.ShowDialog();
            }
            catch
            {
                SafeRestart();
            }
        });
    }

    private void SafeRestart()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
                Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = true });
        }
        catch { }
        Shutdown(-1);
    }
}
